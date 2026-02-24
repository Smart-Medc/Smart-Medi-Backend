using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.MedicalRecord;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.PatientModels;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;


namespace Smart_Medc.Application.Services.Patient
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<MedicalRecordService> _logger;

        private const string MedicalRecordsContainer = "medical-records";
        private static readonly string[] AllowedContentTypes = { "application/pdf", "image/jpeg", "image/png", "image/jpg" };
        private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

        public MedicalRecordService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorage,
            ILogger<MedicalRecordService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<MedicalRecordDto>>> GetPatientRecordsAsync(
            Guid patientId, MedicalRecordQueryDto query, CancellationToken ct = default)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<PagedResult<MedicalRecordDto>>.NotFound("Patient not found");

            var recordsQuery = _unitOfWork.MedicalRecords.QueryNoTracking()
                .Where(r => r.PatientId == patientId && !r.IsDeleted);

            // Filtering
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                recordsQuery = recordsQuery.Where(r =>
                    r.Title.ToLower().Contains(term) ||
                    r.Description != null && r.Description.ToLower().Contains(term) ||
                    r.ProviderName != null && r.ProviderName.ToLower().Contains(term));
            }

            if (query.RecordType.HasValue)
                recordsQuery = recordsQuery.Where(r => r.RecordType == query.RecordType.Value);

            if (query.StartDate.HasValue)
                recordsQuery = recordsQuery.Where(r => r.RecordDate >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                recordsQuery = recordsQuery.Where(r => r.RecordDate <= query.EndDate.Value);

            var totalCount = await recordsQuery.CountAsync(ct);

            // Sorting
            recordsQuery = query.SortBy?.ToLower() switch
            {
                "title" => query.SortDescending
                    ? recordsQuery.OrderByDescending(r => r.Title)
                    : recordsQuery.OrderBy(r => r.Title),
                "createdat" => query.SortDescending
                    ? recordsQuery.OrderByDescending(r => r.CreatedAt)
                    : recordsQuery.OrderBy(r => r.CreatedAt),
                _ => query.SortDescending
                    ? recordsQuery.OrderByDescending(r => r.RecordDate)
                    : recordsQuery.OrderBy(r => r.RecordDate),
            };

            var records = await recordsQuery
                .Include(r => r.Documents.Where(d => !d.IsDeleted))
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var items = records.Select(MapToDto).ToList();

            return ServiceResult<PagedResult<MedicalRecordDto>>.Success(new PagedResult<MedicalRecordDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
            });
        }

        public async Task<ServiceResult<MedicalRecordDetailDto>> GetRecordByIdAsync(
            Guid patientId, Guid recordId, CancellationToken ct = default)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdWithDocumentsAsync(recordId, ct);
            if (record == null || record.PatientId != patientId || record.IsDeleted)
                return ServiceResult<MedicalRecordDetailDto>.NotFound("Medical record not found");

            return ServiceResult<MedicalRecordDetailDto>.Success(MapToDetailDto(record));
        }

        public async Task<ServiceResult<MedicalRecordDto>> CreateRecordAsync(
            Guid patientId, CreateMedicalRecordDto dto, CancellationToken ct = default)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<MedicalRecordDto>.NotFound("Patient not found");

            var record = new MedicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Title = dto.Title,
                Description = dto.Description,
                RecordType = dto.RecordType,
                RecordDate = dto.RecordDate,
                ProviderName = dto.ProviderName,
                OrderedBy = dto.OrderedBy,
                FindingsSummary = dto.FindingsSummary,
                Status = RecordStatus.Final,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MedicalRecords.AddAsync(record, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<MedicalRecordDto>.Success(MapToDto(record));
        }

        public async Task<ServiceResult<MedicalRecordDto>> UpdateRecordAsync(
            Guid patientId, Guid recordId, UpdateMedicalRecordDto dto, CancellationToken ct = default)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, ct);
            if (record == null || record.PatientId != patientId || record.IsDeleted)
                return ServiceResult<MedicalRecordDto>.NotFound("Medical record not found");

            record.Title = dto.Title;
            record.Description = dto.Description;
            record.RecordType = dto.RecordType;
            record.RecordDate = dto.RecordDate;
            record.ProviderName = dto.ProviderName;
            record.OrderedBy = dto.OrderedBy;
            record.FindingsSummary = dto.FindingsSummary;
            record.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.MedicalRecords.UpdateAsync(record, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<MedicalRecordDto>.Success(MapToDto(record));
        }

        public async Task<ServiceResult> DeleteRecordAsync(
            Guid patientId, Guid recordId, CancellationToken ct = default)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdWithDocumentsAsync(recordId, ct);
            if (record == null || record.PatientId != patientId || record.IsDeleted)
                return ServiceResult.NotFound("Medical record not found");

            // Soft-delete the record and its documents
            record.IsDeleted = true;
            record.DeletedAt = DateTime.UtcNow;

            foreach (var doc in record.Documents.Where(d => !d.IsDeleted))
            {
                doc.IsDeleted = true;
                doc.DeletedAt = DateTime.UtcNow;
            }

            // Update patient storage
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient != null)
            {
                var freedBytes = record.Documents.Where(d => !d.IsDeleted).Sum(d => d.FileSizeBytes);
                patient.StorageUsedBytes = Math.Max(0, patient.StorageUsedBytes - freedBytes);
                await _unitOfWork.Patients.UpdateAsync(patient, ct);
            }

            await _unitOfWork.MedicalRecords.UpdateAsync(record, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<DocumentDto>> UploadDocumentAsync(
            Guid patientId, Guid recordId, IFormFile file, CancellationToken ct = default)
        {
            // Validate file
            if (file == null || file.Length == 0)
                return ServiceResult<DocumentDto>.Failure("No file provided");

            if (file.Length > MaxFileSizeBytes)
                return ServiceResult<DocumentDto>.Failure($"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)}MB");

            if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
                return ServiceResult<DocumentDto>.Failure("Invalid file type. Allowed: PDF, JPEG, PNG");

            // Validate record ownership
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, ct);
            if (record == null || record.PatientId != patientId || record.IsDeleted)
                return ServiceResult<DocumentDto>.NotFound("Medical record not found");

            // Check storage quota
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<DocumentDto>.NotFound("Patient not found");

            if (patient.StorageUsedBytes + file.Length > patient.StorageLimitBytes)
                return ServiceResult<DocumentDto>.Failure("Storage quota exceeded");

            // Upload to cloud storage
            using var stream = file.OpenReadStream();
            var uploadResult = await _fileStorage.UploadFileAsync(
                stream, file.FileName, file.ContentType, MedicalRecordsContainer, ct);

            // Determine document format
            var format = file.ContentType.ToLower() switch
            {
                "application/pdf" => DocumentFormat.Pdf,
                "image/jpeg" or "image/jpg" => DocumentFormat.Jpeg,
                "image/png" => DocumentFormat.Png,
                _ => DocumentFormat.Other
            };

            // Save document record
            var document = new MedicalRecordDocument
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = recordId,
                FileName = uploadResult.FileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Format = format,
                FileSizeBytes = uploadResult.FileSizeBytes,
                StoragePath = uploadResult.StoragePath,
                UploadedAt = DateTime.UtcNow
            };

            await _unitOfWork.MedicalRecordDocuments.AddAsync(document, ct);

            // Update patient storage
            patient.StorageUsedBytes += uploadResult.FileSizeBytes;
            await _unitOfWork.Patients.UpdateAsync(patient, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<DocumentDto>.Success(MapToDocumentDto(document));
        }

        public async Task<ServiceResult<Stream>> DownloadDocumentAsync(
            Guid patientId, Guid recordId, Guid documentId, CancellationToken ct = default)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, ct);
            if (record == null || record.PatientId != patientId || record.IsDeleted)
                return ServiceResult<Stream>.NotFound("Medical record not found");

            var documents = await _unitOfWork.MedicalRecordDocuments
                .GetByMedicalRecordIdAsync(recordId, false, ct);
            var document = documents.FirstOrDefault(d => d.Id == documentId);

            if (document == null || document.IsDeleted)
                return ServiceResult<Stream>.NotFound("Document not found");

            var stream = await _fileStorage.DownloadFileAsync(
                MedicalRecordsContainer, document.StoragePath, ct);

            return ServiceResult<Stream>.Success(stream);
        }

        public async Task<ServiceResult> DeleteDocumentAsync(
            Guid patientId, Guid recordId, Guid documentId, CancellationToken ct = default)
        {
            var record = await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, ct);
            if (record == null || record.PatientId != patientId || record.IsDeleted)
                return ServiceResult.NotFound("Medical record not found");

            var documents = await _unitOfWork.MedicalRecordDocuments
                .GetByMedicalRecordIdAsync(recordId, false, ct);
            var document = documents.FirstOrDefault(d => d.Id == documentId);

            if (document == null || document.IsDeleted)
                return ServiceResult.NotFound("Document not found");

            // Soft delete
            document.IsDeleted = true;
            document.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.MedicalRecordDocuments.UpdateAsync(document, ct);

            // Update storage
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient != null)
            {
                patient.StorageUsedBytes = Math.Max(0, patient.StorageUsedBytes - document.FileSizeBytes);
                await _unitOfWork.Patients.UpdateAsync(patient, ct);
            }

            // Also delete from cloud storage
            await _fileStorage.DeleteFileAsync(MedicalRecordsContainer, document.StoragePath, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<MedicalRecordStatisticsDto>> GetStatisticsAsync(
            Guid patientId, CancellationToken ct = default)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(patientId, ct);
            if (patient == null)
                return ServiceResult<MedicalRecordStatisticsDto>.NotFound("Patient not found");

            var records = await _unitOfWork.MedicalRecords
                .GetByPatientIdAsync(patientId, false, ct);

            var recordsList = records.ToList();

            return ServiceResult<MedicalRecordStatisticsDto>.Success(new MedicalRecordStatisticsDto
            {
                TotalRecords = recordsList.Count,
                LabReports = recordsList.Count(r => r.RecordType == MedicalRecordType.LabReport),
                Imaging = recordsList.Count(r => r.RecordType == MedicalRecordType.Imaging),
                ConsultationNotes = recordsList.Count(r => r.RecordType == MedicalRecordType.ConsultationNotes),
                Immunizations = recordsList.Count(r => r.RecordType == MedicalRecordType.Immunization),
                Other = recordsList.Count(r => r.RecordType != MedicalRecordType.LabReport &&
                    r.RecordType != MedicalRecordType.Imaging &&
                    r.RecordType != MedicalRecordType.ConsultationNotes &&
                    r.RecordType != MedicalRecordType.Immunization),
                StorageUsedBytes = patient.StorageUsedBytes,
                StorageLimitBytes = patient.StorageLimitBytes,
                StorageUsedPercentage = patient.StorageLimitBytes > 0
                    ? Math.Round((double)patient.StorageUsedBytes / patient.StorageLimitBytes * 100, 2)
                    : 0
            });
        }

        // --- Mapping Helpers ---
        private static MedicalRecordDto MapToDto(MedicalRecord record) => new()
        {
            Id = record.Id,
            Title = record.Title,
            Description = record.Description,
            RecordType = record.RecordType,
            RecordDate = record.RecordDate,
            ProviderName = record.ProviderName,
            OrderedBy = record.OrderedBy,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            FindingsSummary = record.FindingsSummary,
            DocumentCount = record.Documents?.Count(d => !d.IsDeleted) ?? 0,
            TotalDocumentSize = record.Documents?.Where(d => !d.IsDeleted).Sum(d => d.FileSizeBytes) ?? 0
        };

        private static MedicalRecordDetailDto MapToDetailDto(MedicalRecord record) => new()
        {
            Id = record.Id,
            Title = record.Title,
            Description = record.Description,
            RecordType = record.RecordType,
            RecordDate = record.RecordDate,
            ProviderName = record.ProviderName,
            OrderedBy = record.OrderedBy,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            FindingsSummary = record.FindingsSummary,
            DocumentCount = record.Documents?.Count(d => !d.IsDeleted) ?? 0,
            TotalDocumentSize = record.Documents?.Where(d => !d.IsDeleted).Sum(d => d.FileSizeBytes) ?? 0,
            Documents = record.Documents?.Where(d => !d.IsDeleted).Select(MapToDocumentDto).ToList() ?? new()
        };

        private static DocumentDto MapToDocumentDto(MedicalRecordDocument doc) => new()
        {
            Id = doc.Id,
            FileName = doc.FileName,
            OriginalFileName = doc.OriginalFileName,
            ContentType = doc.ContentType,
            Format = doc.Format,
            FileSizeBytes = doc.FileSizeBytes,
            UploadedAt = doc.UploadedAt
        };
    }
}
