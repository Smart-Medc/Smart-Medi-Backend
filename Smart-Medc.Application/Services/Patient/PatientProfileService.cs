using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.MedicalRecord;
using Smart_Medc.Application.DTOs.Patient;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Application.Interfaces.Storage;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services.Patient
{
    public class PatientProfileService : IPatientProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PatientProfileService> _logger;
        private readonly ILocalFileStorageService _fileStorage;
        private readonly IMedicalRecordService _medicalRecordService;
        private readonly IMedicationService _medicationService;
        private readonly IPdfExportService? _pdfExportService;
        private readonly INotificationPreferencesService _notificationPreferencesService;

        private const string ProfilePhotosFolder = "patient-profiles";
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxProfilePhotoSizeBytes = 5 * 1024 * 1024;

        public PatientProfileService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PatientProfileService> logger,
            ILocalFileStorageService fileStorage,
            IMedicalRecordService medicalRecordService,
            IMedicationService medicationService,
            INotificationPreferencesService notificationPreferencesService,
            IPdfExportService? pdfExportService = null)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _fileStorage = fileStorage;
            _medicalRecordService = medicalRecordService;
            _medicationService = medicationService;
            _notificationPreferencesService = notificationPreferencesService;
            _pdfExportService = pdfExportService;
        }

        public async Task<ServiceResult<PatientProfileDto>> GetPatientProfileAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);

                if (patient?.User == null)
                {
                    _logger.LogWarning("Patient profile not found for user {UserId}", userId);
                    return ServiceResult<PatientProfileDto>.NotFound("Patient profile not found");
                }

                var profileDto = MapPatientToProfileDto(patient);
                return ServiceResult<PatientProfileDto>.Success(profileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient profile for user {UserId}", userId);
                return ServiceResult<PatientProfileDto>.Failure("An error occurred while retrieving the profile");
            }
        }
        public async Task<ServiceResult<PatientProfileDto>> UpdatePersonalInfoAsync(
            Guid userId,
            UpdatePersonalInfoRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate date of birth
                if (request.DateOfBirth >= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid date of birth provided for user {UserId}", userId);
                    return ServiceResult<PatientProfileDto>.Failure("Date of birth cannot be in the future", 400);
                }

                var age = CalculateAge(request.DateOfBirth);
                if (age < 0 || age > 150)
                {
                    _logger.LogWarning("Invalid age calculated for user {UserId}: {Age}", userId, age);
                    return ServiceResult<PatientProfileDto>.Failure("Invalid age based on date of birth", 400);
                }

                // Get patient
                var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
                if (patient?.User == null)
                {
                    _logger.LogWarning("Patient not found for user {UserId}", userId);
                    return ServiceResult<PatientProfileDto>.NotFound("Patient profile not found");
                }

                // Update patient and user information
                patient.DateOfBirth = request.DateOfBirth;
                patient.Gender = request.Gender;
                patient.BloodType = request.BloodType;
                patient.Address = request.Address;

                patient.User.FirstName = request.FirstName;
                patient.User.LastName = request.LastName;
                patient.User.PhoneNumber = request.PhoneNumber;
                patient.User.UpdatedAt = DateTime.UtcNow;

                patient.Allergies = request.Allergies;
                patient.HasNoKnownAllergies = request.HasNoKnownAllergies;

                patient.EmergencyContactName = request.ContactName;
                patient.EmergencyContactPhone = request.ContactPhone;
                patient.EmergencyContactRelationship = request.Relationship;

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Personal information updated for user {UserId}", userId);

                var profileDto = MapPatientToProfileDto(patient);
                return ServiceResult<PatientProfileDto>.Success(profileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating personal information for user {UserId}", userId);
                return ServiceResult<PatientProfileDto>.Failure("An error occurred while updating personal information");
            }
        }


        public async Task<ServiceResult<ProfilePhotoResponse>> ChangeProfilePhotoAsync(
            Guid userId,
            IFormFile photoFile,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (isValid, errorMessage) = _fileStorage.ValidateFile(
                    photoFile,
                    AllowedImageExtensions,
                    MaxProfilePhotoSizeBytes);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid profile photo for user {UserId}: {ErrorMessage}", userId, errorMessage);
                    return ServiceResult<ProfilePhotoResponse>.Failure(errorMessage ?? "Invalid file", 400);
                }

                var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
                if (patient?.User == null)
                {
                    _logger.LogWarning("Patient not found for user {UserId}", userId);
                    return ServiceResult<ProfilePhotoResponse>.NotFound("Patient profile not found");
                }

                var oldPhotoUrl = patient.User.ProfileImageUrl;

                // Upload new photo
                var extension = Path.GetExtension(photoFile.FileName);
                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                var (uploadSuccess, filePath, uploadErrorMessage) = await _fileStorage.UploadFileAsync(
                    photoFile,
                    ProfilePhotosFolder,
                    fileName);

                if (!uploadSuccess)
                {
                    _logger.LogError("Failed to upload profile photo for user {UserId}: {ErrorMessage}", userId, uploadErrorMessage);
                    return ServiceResult<ProfilePhotoResponse>.Failure(
                        uploadErrorMessage ?? "Failed to upload photo", 400);
                }

                patient.User.ProfileImageUrl = _fileStorage.GetFileUrl(filePath);
                patient.User.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(oldPhotoUrl) && oldPhotoUrl != patient.User.ProfileImageUrl)
                {
                    var oldPhotoPath = oldPhotoUrl?.Replace("/uploads/", "").Replace("\\", "/");
                    if (!string.IsNullOrEmpty(oldPhotoPath))
                    {
                        _ = await _fileStorage.DeleteFileAsync(oldPhotoPath);
                        _logger.LogInformation("Deleted old profile photo: {PhotoUrl}", oldPhotoUrl);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Profile photo changed for user {UserId}. Old: {OldPhotoUrl}, New: {NewPhotoUrl}",
                    userId, oldPhotoUrl, patient.User.ProfileImageUrl);

                var response = new ProfilePhotoResponse
                {
                    PhotoUrl = patient.User.ProfileImageUrl,
                    Message = "Profile photo changed successfully",
                    OldPhotoUrl = oldPhotoUrl
                };

                return ServiceResult<ProfilePhotoResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing profile photo for user {UserId}", userId);
                return ServiceResult<ProfilePhotoResponse>.Failure("An error occurred while changing the profile photo");
            }
        }
        public async Task<ServiceResult<ProfilePhotoResponse>> RemoveProfilePhotoAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
                if (patient?.User == null)
                {
                    _logger.LogWarning("Patient not found for user {UserId}", userId);
                    return ServiceResult<ProfilePhotoResponse>.NotFound("Patient profile not found");
                }

                var oldPhotoUrl = patient.User.ProfileImageUrl;

                if (string.IsNullOrEmpty(oldPhotoUrl))
                {
                    _logger.LogWarning("No profile photo to remove for user {UserId}", userId);
                    return ServiceResult<ProfilePhotoResponse>.Failure(
                        "No profile photo to remove", 400);
                }

                var photoPath = oldPhotoUrl.Replace("/uploads/", "").Replace("\\", "/");
                var deleteSuccess = await _fileStorage.DeleteFileAsync(photoPath);

                if (!deleteSuccess)
                {
                    _logger.LogWarning("Failed to delete profile photo file: {PhotoPath}", photoPath);
                }

                patient.User.ProfileImageUrl = null;
                patient.User.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Profile photo removed for user {UserId}. Deleted: {PhotoUrl}", userId, oldPhotoUrl);

                var response = new ProfilePhotoResponse
                {
                    PhotoUrl = null,
                    Message = "Profile photo removed successfully",
                    OldPhotoUrl = oldPhotoUrl
                };

                return ServiceResult<ProfilePhotoResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing profile photo for user {UserId}", userId);
                return ServiceResult<ProfilePhotoResponse>.Failure("An error occurred while removing the profile photo");
            }
        }


        public async Task<ServiceResult<MedicalRecordsExportDto>> ExportMedicalRecordsAsync(
             Guid userId,
             ExportMedicalRecordsRequest request,
             CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
                {
                    _logger.LogWarning("Invalid date range for export: FromDate > ToDate for user {UserId}", userId);
                    return ServiceResult<MedicalRecordsExportDto>.Failure("FromDate cannot be greater than ToDate", 400);
                }

                var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
                if (patient?.User == null)
                {
                    _logger.LogWarning("Patient not found for export for user {UserId}", userId);
                    return ServiceResult<MedicalRecordsExportDto>.NotFound("Patient profile not found");
                }

                var exportData = new MedicalRecordsExportDto
                {
                    Metadata = new ExportMetadata
                    {
                        ExportFormat = request.Format.ToLower(),
                        ExportedAt = DateTime.UtcNow,
                        ExportedBy = "Patient"
                    }
                };

                if (request.IncludeProfileInfo || request.IncludeEmergencyContact)
                {
                    exportData.PatientInfo = new PatientExportInfo
                    {
                        FullName = patient.User.FullName,
                        Email = patient.User.Email ?? string.Empty,
                        PhoneNumber = patient.User.PhoneNumber,
                        DateOfBirth = patient.DateOfBirth,
                        Age = CalculateAge(patient.DateOfBirth),
                        Gender = patient.Gender.ToString(),
                        BloodType = patient.BloodType?.ToString(),
                        Address = patient.Address,
                        Allergies = patient.Allergies,
                        HasNoKnownAllergies = patient.HasNoKnownAllergies,
                        EmergencyContactName = patient.EmergencyContactName,
                        EmergencyContactPhone = patient.EmergencyContactPhone,
                        EmergencyContactRelationship = patient.EmergencyContactRelationship.ToString()
                    };
                }

                if (request.IncludeMedicalRecords)
                {
                    var recordsQuery = new MedicalRecordQueryDto
                    {
                        PageNumber = 1,
                        PageSize = 1000
                    };

                    var recordsResult = await _medicalRecordService.GetPatientRecordsAsync(
                        patient.Id, recordsQuery, cancellationToken);

                    if (recordsResult.IsSuccess && recordsResult.Data?.Items != null)
                    {
                        var records = recordsResult.Data.Items.Cast<MedicalRecordDto>().ToList();

                        if (request.FromDate.HasValue || request.ToDate.HasValue)
                        {
                            records = records.Where(r =>
                                (!request.FromDate.HasValue || r.RecordDate >= request.FromDate.Value) &&
                                (!request.ToDate.HasValue || r.RecordDate <= request.ToDate.Value)
                            ).ToList();
                        }

                        foreach (var record in records)
                        {
                            var detailResult = await _medicalRecordService.GetRecordByIdAsync(
                                patient.Id, record.Id, cancellationToken);

                            if (detailResult.IsSuccess && detailResult.Data != null)
                            {
                                var exportRecord = new MedicalRecordExportDto
                                {
                                    Id = detailResult.Data.Id,
                                    Title = detailResult.Data.Title,
                                    Description = detailResult.Data.Description,
                                    RecordType = detailResult.Data.RecordTypeName,
                                    RecordDate = detailResult.Data.RecordDate,
                                    ProviderName = detailResult.Data.ProviderName,
                                    OrderedBy = detailResult.Data.OrderedBy,
                                    Status = detailResult.Data.StatusName,
                                    FindingsSummary = detailResult.Data.FindingsSummary,
                                    CreatedAt = detailResult.Data.CreatedAt,
                                    UpdatedAt = detailResult.Data.UpdatedAt
                                };

                                if (detailResult.Data.Documents != null)
                                {
                                    foreach (var doc in detailResult.Data.Documents)
                                    {
                                        exportRecord.Documents.Add(new DocumentExportDto
                                        {
                                            Id = doc.Id,
                                            FileName = doc.FileName,
                                            OriginalFileName = doc.OriginalFileName,
                                            ContentType = doc.ContentType,
                                            Format = doc.Format.ToString(),
                                            FileSizeBytes = doc.FileSizeBytes,
                                            UploadedAt = doc.UploadedAt
                                        });
                                    }
                                }

                                exportData.MedicalRecords.Add(exportRecord);
                            }
                        }
                    }
                }

                if (request.IncludeMedications)
                {
                    var medicationsResult = await _medicationService.GetMedicationsAsync(
                        patient.Id, includeInactive: true, cancellationToken);

                    if (medicationsResult.IsSuccess && medicationsResult.Data != null)
                    {
                        foreach (var med in medicationsResult.Data)
                        {
                            var endDate = med.EndDate;
                            var isActive = endDate == null || endDate > DateTime.UtcNow;

                            exportData.Medications.Add(new MedicationExportDto
                            {
                                Id = med.Id,
                                Name = med.Name,
                                Dosage = med.Dosage,
                                Frequency = med.Frequency,
                                Route = med.Route.ToString(),
                                Instructions = med.Instructions,
                                StartDate = med.StartDate,
                                EndDate = med.EndDate,
                                IsActive = isActive,
                                PrescribingDoctor = med.PrescribingDoctor
                            });
                        }
                    }
                }

                exportData.Summary = new ExportSummary
                {
                    TotalMedicalRecords = exportData.MedicalRecords.Count,
                    TotalDocuments = exportData.MedicalRecords.Sum(r => r.Documents.Count),
                    TotalDocumentSizeBytes = exportData.MedicalRecords.Sum(r => r.Documents.Sum(d => d.FileSizeBytes)),
                    ActiveMedications = exportData.Medications.Count(m => m.IsActive),
                    TotalMedications = exportData.Medications.Count,
                    DateRangeFrom = request.FromDate?.ToString("yyyy-MM-dd") ?? "N/A",
                    DateRangeTo = request.ToDate?.ToString("yyyy-MM-dd") ?? "N/A"
                };

                _logger.LogInformation(
                    "Medical records exported for user {UserId}. Records: {RecordCount}, Medications: {MedicationCount}",
                    userId, exportData.MedicalRecords.Count, exportData.Medications.Count);

                return ServiceResult<MedicalRecordsExportDto>.Success(exportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting medical records for user {UserId}", userId);
                return ServiceResult<MedicalRecordsExportDto>.Failure("An error occurred while exporting medical records");
            }
        }
        public async Task<ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>> ExportMedicalRecordsAsFileAsync(
            Guid userId,
            ExportMedicalRecordsRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var exportResult = await ExportMedicalRecordsAsync(userId, request, cancellationToken);

                if (!exportResult.IsSuccess)
                {
                    return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Failure(
                        exportResult.ErrorMessage ?? "Failed to export records", exportResult.StatusCode);
                }

                var exportData = exportResult.Data;

                if (request.Format.ToLower() == "pdf")
                {
                    if (_pdfExportService == null)
                    {
                        _logger.LogWarning("PDF export service not configured for user {UserId}", userId);
                        return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Failure(
                            "PDF export is not available. Please try JSON format.", 400);
                    }

                    try
                    {
                        var fileName = $"MedicalRecords_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                        var pdfBytes = await _pdfExportService.GenerateMedicalRecordsPdfAsync(
                            exportData, fileName, cancellationToken);

                        _logger.LogInformation("PDF export generated for user {UserId}. Size: {Size} bytes",
                            userId, pdfBytes.Length);

                        return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Success(
                            (pdfBytes, fileName, "application/pdf"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating PDF for user {UserId}", userId);
                        return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Failure(
                            "Failed to generate PDF export", 500);
                    }
                }
                else
                {
                    try
                    {
                        var jsonOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        };

                        var jsonString = JsonSerializer.Serialize(exportData, jsonOptions);
                        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                        var fileName = $"MedicalRecords_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

                        _logger.LogInformation("JSON export generated for user {UserId}. Size: {Size} bytes",
                            userId, jsonBytes.Length);

                        return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Success(
                            (jsonBytes, fileName, "application/json"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating JSON for user {UserId}", userId);
                        return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Failure(
                            "Failed to generate JSON export", 500);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting medical records as file for user {UserId}", userId);
                return ServiceResult<(byte[] FileBytes, string FileName, string ContentType)>.Failure(
                    "An error occurred while exporting medical records as file");
            }
        }


        public async Task<ServiceResult<NotificationPreferencesResponse>> GetNotificationPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _notificationPreferencesService.GetAllPreferencesAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
                return ServiceResult<NotificationPreferencesResponse>.Failure("An error occurred while retrieving notification preferences");
            }
        }

        public async Task<ServiceResult<NotificationPreferenceDto>> UpdateNotificationPreferenceAsync(
            Guid userId,
            UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _notificationPreferencesService.UpdatePreferenceAsync(userId, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preference for user {UserId}", userId);
                return ServiceResult<NotificationPreferenceDto>.Failure("An error occurred while updating notification preference");
            }
        }

        public async Task<ServiceResult<NotificationPreferencesResponse>> UpdateAllNotificationPreferencesAsync(
            Guid userId,
            UpdateAllNotificationPreferencesRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _notificationPreferencesService.UpdateAllPreferencesAsync(userId, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all notification preferences for user {UserId}", userId);
                return ServiceResult<NotificationPreferencesResponse>.Failure("An error occurred while updating notification preferences");
            }
        }

        private PatientProfileDto MapPatientToProfileDto(Domain.Entities.PatientModels.Patient patient)
        {
            var age = CalculateAge(patient.DateOfBirth);

            return new PatientProfileDto
            {
                UserId = patient.UserId,
                PatientId = patient.Id,
                Email = patient.User!.Email ?? string.Empty,
                FirstName = patient.User.FirstName,
                LastName = patient.User.LastName,
                FullName = patient.User.FullName,
                ProfileImageUrl = patient.User.ProfileImageUrl,
                PhoneNumber = patient.User.PhoneNumber,
                IsPhoneVerified = patient.User.IsPhoneVerified,
                DateOfBirth = patient.DateOfBirth,
                Age = age,
                Gender = patient.Gender.ToString(),
                BloodType = patient.BloodType?.ToString(),
                Address = patient.Address,
                Allergies = patient.Allergies,
                HasNoKnownAllergies = patient.HasNoKnownAllergies,
                EmergencyContactName = patient.EmergencyContactName,
                EmergencyContactPhone = patient.EmergencyContactPhone,
                EmergencyContactRelationship = patient.EmergencyContactRelationship.ToString(),
                CreatedAt = patient.CreatedAt,
                UpdatedAt = patient.UpdatedAt
            };
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow;
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}