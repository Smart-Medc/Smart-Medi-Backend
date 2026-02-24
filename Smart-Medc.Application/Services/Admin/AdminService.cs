using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common.Results;
using Smart_Medc.Application.DTOs.Admin;
using Smart_Medc.Application.DTOs.Auth;
using Smart_Medc.Application.Interfaces.Auth;
using Smart_Medc.Application.Interfaces.Services;
using Smart_Medc.Application.Interfaces.Storage;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;
using Smart_Medc.Domain.Interfaces.Services.Auth;

namespace Smart_Medc.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILocalFileStorageService _fileStorage;
        private readonly ILogger<AdminService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;

        public AdminService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILocalFileStorageService fileStorage,
            ILogger<AdminService> logger,
            ITokenService tokenService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _fileStorage = fileStorage;
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
        }

        #region Dashboard Statistics

        public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var allPatients = await _unitOfWork.Patients.GetAllAsync();
                var activePatients = 0;
                var deletedPatients = 0;
                var newPatientsThisMonth = 0;
                DateTime? lastPatientRegistration = null;

                foreach (var patient in allPatients)
                {
                    var user = await _userManager.FindByIdAsync(patient.UserId.ToString());
                    if (user != null)
                    {
                        if (user.IsActive)
                        {
                            activePatients++;
                            if (patient.CreatedAt >= startOfMonth)
                                newPatientsThisMonth++;
                        }
                        else
                        {
                            deletedPatients++;
                        }

                        if (lastPatientRegistration == null || patient.CreatedAt > lastPatientRegistration)
                            lastPatientRegistration = patient.CreatedAt;
                    }
                }

                var stats = new DashboardStatsDto
                {
                    TotalActivePatients = activePatients,
                    TotalDeletedPatients = deletedPatients,
                    NewPatientsThisMonth = newPatientsThisMonth,

                    TotalPendingOrganizations = await _unitOfWork.Organizations.CountAsync(
                        o => o.VerificationStatus == VerificationStatus.Pending),
                    TotalApprovedOrganizations = await _unitOfWork.Organizations.CountAsync(
                        o => o.VerificationStatus == VerificationStatus.Verified),
                    TotalRejectedOrganizations = await _unitOfWork.Organizations.CountAsync(
                        o => o.VerificationStatus == VerificationStatus.Rejected),
                    NewOrganizationsThisMonth = await _unitOfWork.Organizations.CountAsync(
                        o => o.CreatedAt >= startOfMonth),

                    LastPatientRegistration = lastPatientRegistration,

                    LastOrganizationRegistration = (await _unitOfWork.Organizations
                        .GetAllAsync())
                        .OrderByDescending(o => o.CreatedAt)
                        .FirstOrDefault()?.CreatedAt,

                    LastOrganizationApproval = (await _unitOfWork.Organizations
                        .FindAsync(o => o.VerificationStatus == VerificationStatus.Verified))
                        .OrderByDescending(o => o.VerifiedAt)
                        .FirstOrDefault()?.VerifiedAt
                };

                return Result<DashboardStatsDto>.Success(stats, "Dashboard statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard statistics");
                return Result<DashboardStatsDto>.Failure("Failed to retrieve dashboard statistics");
            }
        }

        #endregion

        #region Patient Management

        public async Task<Result<List<PatientListItemDto>>> GetAllActivePatientsAsync()
        {
            try
            {
                var allPatients = await _unitOfWork.Patients.GetAllAsync();
                var patientDtos = new List<PatientListItemDto>();

                foreach (var patient in allPatients)
                {
                    var user = await _userManager.FindByIdAsync(patient.UserId.ToString());

                    if (user != null && user.IsActive)
                    {
                        patientDtos.Add(new PatientListItemDto
                        {
                            Id = patient.Id,
                            UserId = patient.UserId,
                            FullName = $"{user.FirstName} {user.LastName}",
                            Email = user.Email ?? string.Empty,
                            PhoneNumber = user.PhoneNumber ?? string.Empty,
                            Age = CalculateAge(patient.DateOfBirth),
                            Gender = patient.Gender.ToString(),
                            DateOfBirth = patient.DateOfBirth,
                            RegisteredAt = patient.CreatedAt,
                            IsActive = user.IsActive,
                            //DeletedAt = patient.DeletedAt,
                            //DeletedBy = patient.DeletedBy,
                            EmergencyContactName = patient.EmergencyContactName,
                            EmergencyContactPhone = patient.EmergencyContactPhone,
                            EmergencyContactRelationship = patient.EmergencyContactRelationship.ToString()
                        });
                    }
                }

                return Result<List<PatientListItemDto>>.Success(
                    patientDtos.OrderByDescending(p => p.RegisteredAt).ToList(),
                    $"Retrieved {patientDtos.Count} active patients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active patients");
                return Result<List<PatientListItemDto>>.Failure("Failed to retrieve active patients");
            }
        }

        public async Task<Result<List<PatientListItemDto>>> GetAllDeletedPatientsAsync()
        {
            try
            {
                var allPatients = await _unitOfWork.Patients.GetAllAsync();
                var patientDtos = new List<PatientListItemDto>();

                foreach (var patient in allPatients)
                {
                    var user = await _userManager.FindByIdAsync(patient.UserId.ToString());

                    if (user != null && !user.IsActive)
                    {
                        patientDtos.Add(new PatientListItemDto
                        {
                            Id = patient.Id,
                            UserId = patient.UserId,
                            FullName = $"{user.FirstName} {user.LastName}",
                            Email = user.Email ?? string.Empty,
                            PhoneNumber = user.PhoneNumber ?? string.Empty,
                            Age = CalculateAge(patient.DateOfBirth),
                            Gender = patient.Gender.ToString(),
                            DateOfBirth = patient.DateOfBirth,
                            RegisteredAt = patient.CreatedAt,
                            IsActive = user.IsActive,
                            //DeletedAt = patient.DeletedAt,
                            //DeletedBy = patient.DeletedBy,
                            EmergencyContactName = patient.EmergencyContactName,
                            EmergencyContactPhone = patient.EmergencyContactPhone,
                            EmergencyContactRelationship = patient.EmergencyContactRelationship.ToString()
                        });
                    }
                }

                return Result<List<PatientListItemDto>>.Success(
                    patientDtos.OrderByDescending(p => p.DeletedAt).ToList(),
                    $"Retrieved {patientDtos.Count} deleted patients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deleted patients");
                return Result<List<PatientListItemDto>>.Failure("Failed to retrieve deleted patients");
            }
        }

        public async Task<Result> SoftDeletePatientAsync(Guid patientId, Guid adminUserId)
        {
            try
            {
                var patient = await _unitOfWork.Patients.GetByIdAsync(patientId);
                if (patient == null)
                    return Result.Failure("Patient not found");

                var user = await _userManager.FindByIdAsync(patient.UserId.ToString());
                if (user == null)
                    return Result.Failure("Patient user not found");

                if (!user.IsActive)
                    return Result.Failure("Patient is already deleted");

                // { Enable it later } //
                //var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
                //if (adminUser == null)
                //    return Result.Failure("Admin user not found");

                // Update Patient entity (for tracking)
                //patient.DeletedAt = DateTime.UtcNow;
                //patient.DeletedBy = $"{adminUser.FirstName} {adminUser.LastName}";
                await _unitOfWork.Patients.UpdateAsync(patient);

                user.IsActive = false; // Soft delete
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Patient {PatientId} soft deleted by admin {AdminId}", patientId, adminUserId);

                return Result.Success("Patient deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting patient {PatientId}", patientId);
                return Result.Failure("Failed to delete patient");
            }
        }

        public async Task<Result> RestorePatientAsync(Guid patientId, Guid adminUserId)
        {
            try
            {
                var patient = await _unitOfWork.Patients.GetByIdAsync(patientId);
                if (patient == null)
                    return Result.Failure("Patient not found");

                var user = await _userManager.FindByIdAsync(patient.UserId.ToString());
                if (user == null)
                    return Result.Failure("Patient user not found");

                if (user.IsActive)
                    return Result.Failure("Patient is already active");

                //patient.DeletedAt = null;
                //patient.DeletedBy = null;
                await _unitOfWork.Patients.UpdateAsync(patient);

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Patient {PatientId} restored by admin {AdminId}", patientId, adminUserId);

                return Result.Success("Patient restored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring patient {PatientId}", patientId);
                return Result.Failure("Failed to restore patient");
            }
        }

        #endregion

        #region Organization Management

        public async Task<Result<List<OrganizationListItemDto>>> GetOrganizationsByStatusAsync(string status)
        {
            try
            {
                if (!Enum.TryParse<VerificationStatus>(status, true, out var verificationStatus))
                    return Result<List<OrganizationListItemDto>>.Failure("Invalid verification status");

                var organizations = await _unitOfWork.Organizations.FindAsync(
                    o => o.VerificationStatus == verificationStatus);

                var orgDtos = new List<OrganizationListItemDto>();

                foreach (var org in organizations)
                {
                    var user = await _userManager.FindByIdAsync(org.UserId.ToString());
                    if (user != null)
                    {
                        var documentCount = await _unitOfWork.OrganizationDocuments.CountAsync(
                            d => d.OrganizationId == org.Id);

                        orgDtos.Add(new OrganizationListItemDto
                        {
                            Id = org.Id,
                            UserId = org.UserId,
                            Name = org.Name,
                            Type = org.Type.ToString(),
                            ContactEmail = user.Email ?? string.Empty,
                            ContactPhone = user.PhoneNumber ?? string.Empty,
                            Address = org.Address,
                            City = org.City ?? string.Empty,
                            State = org.State ?? string.Empty,
                            ZipCode = org.ZipCode ?? string.Empty,
                            Country = org.Country ?? string.Empty,
                            VerificationStatus = org.VerificationStatus.ToString(),
                            RegisteredAt = org.CreatedAt,
                            VerifiedAt = org.VerifiedAt,
                            RejectionReason = org.RejectionReason,
                            DocumentCount = documentCount,
                            Website = org.Website,
                            SSN = org.SSN,
                            IsActive = user.IsActive
                        });
                    }
                }

                return Result<List<OrganizationListItemDto>>.Success(
                    orgDtos.OrderByDescending(o => o.RegisteredAt).ToList(),
                    $"Retrieved {orgDtos.Count} organizations with status {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizations by status {Status}", status);
                return Result<List<OrganizationListItemDto>>.Failure("Failed to retrieve organizations");
            }
        }

        public async Task<Result<OrganizationDetailsDto>> GetOrganizationDetailsAsync(Guid organizationId)
        {
            try
            {
                var org = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (org == null)
                    return Result<OrganizationDetailsDto>.Failure("Organization not found");

                var user = await _userManager.FindByIdAsync(org.UserId.ToString());
                if (user == null)
                    return Result<OrganizationDetailsDto>.Failure("Organization user not found");

                var documents = await _unitOfWork.OrganizationDocuments.FindAsync(
                    d => d.OrganizationId == organizationId);

                var details = new OrganizationDetailsDto
                {
                    Id = org.Id,
                    UserId = org.UserId,
                    Name = org.Name,
                    Type = org.Type.ToString(),
                    Description = org.Description ?? string.Empty,
                    ContactEmail = user.Email ?? string.Empty,
                    ContactPhone = user.PhoneNumber ?? string.Empty,
                    ContactPerson = $"{user.FirstName} {user.LastName}",
                    Address = org.Address,
                    City = org.City ?? string.Empty,
                    State = org.State ?? string.Empty,
                    ZipCode = org.ZipCode ?? string.Empty,
                    Country = org.Country ?? string.Empty,
                    VerificationStatus = org.VerificationStatus.ToString(),
                    RegisteredAt = org.CreatedAt,
                    VerifiedAt = org.VerifiedAt,
                    RejectionReason = org.RejectionReason,
                    Website = org.Website,
                    SSN = org.SSN,
                    IsActive = user.IsActive,

                    Documents = documents.Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        OrganizationId = d.OrganizationId,
                        DocumentName = d.DocumentName,
                        DocumentType = d.DocumentType.ToString(),
                        FileName = d.FileName,
                        FileUrl = _fileStorage.GetFileUrl(d.StoragePath),
                        ContentType = d.ContentType,
                        FileSizeBytes = d.FileSizeBytes,
                        VerificationStatus = d.VerificationStatus.ToString(),
                        UploadedAt = d.UploadedAt,
                        VerifiedAt = d.VerifiedAt
                    }).ToList()
                };

                return Result<OrganizationDetailsDto>.Success(details, "Organization details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization details {OrganizationId}", organizationId);
                return Result<OrganizationDetailsDto>.Failure("Failed to retrieve organization details");
            }
        }

        public async Task<Result> ApproveOrganizationAsync(ApproveOrganizationRequest request, Guid adminUserId)
        {
            try
            {
                var org = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId);
                if (org == null)
                    return Result.Failure("Organization not found");

                if (org.VerificationStatus != VerificationStatus.Pending)
                    return Result.Failure($"Organization is already {org.VerificationStatus}");

                // { Enable it later } //
                //var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
                //if (adminUser == null)
                //    return Result.Failure("Admin user not found");

                org.VerificationStatus = VerificationStatus.Verified;
                org.VerifiedAt = DateTime.UtcNow;
                org.RejectionReason = null;

                await _unitOfWork.Organizations.UpdateAsync(org);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    await _tokenService.RevokeAllUserRefreshTokensAsync(
                        org.UserId,
                        "Organization approved - must login with new verified status");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revoking tokens for organization {OrganizationId}", request.OrganizationId);
                }

                var orgUser = await _userManager.FindByIdAsync(org.UserId.ToString());
                if (orgUser?.Email != null)
                {
                    await _emailService.SendOrganizationApprovedEmailAsync(
                        orgUser.Email,
                        org.Name,
                        orgUser.FirstName);
                }

                _logger.LogInformation(
                    "Organization {OrganizationId} approved by admin {AdminId}",
                    request.OrganizationId,
                    adminUserId);

                return Result.Success("Organization approved successfully. The organization can now login.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving organization {OrganizationId}", request.OrganizationId);
                return Result.Failure("Failed to approve organization");
            }
        }

        public async Task<Result> RejectOrganizationAsync(RejectOrganizationRequest request, Guid adminUserId)
        {
            try
            {
                var org = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId);
                if (org == null)
                    return Result.Failure("Organization not found");

                if (org.VerificationStatus != VerificationStatus.Pending)
                    return Result.Failure($"Organization is already {org.VerificationStatus}");

                // { Enable it later } //
                //var adminUser = await _userManager.FindByIdAsync(adminUserId.ToString());
                //if (adminUser == null)
                //    return Result.Failure("Admin user not found");

                org.VerificationStatus = VerificationStatus.Rejected;
                org.VerifiedAt = DateTime.UtcNow;
                org.RejectionReason = request.RejectionReason;

                await _unitOfWork.Organizations.UpdateAsync(org);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    await _tokenService.RevokeAllUserRefreshTokensAsync(
                        org.UserId,
                        "Organization rejected - login disabled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revoking tokens for organization {OrganizationId}", request.OrganizationId);
                }

                var orgUser = await _userManager.FindByIdAsync(org.UserId.ToString());
                if (orgUser?.Email != null)
                {
                    await _emailService.SendOrganizationRejectedEmailAsync(
                        orgUser.Email,
                        org.Name,
                        orgUser.FirstName,
                        request.RejectionReason);
                }

                _logger.LogInformation(
                    "Organization {OrganizationId} rejected by admin {AdminId}",
                    request.OrganizationId,
                    adminUserId);

                return Result.Success("Organization rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting organization {OrganizationId}", request.OrganizationId);
                return Result.Failure("Failed to reject organization");
            }
        }

        #endregion

        #region Notifications

        public async Task SendOrganizationRegistrationNotificationAsync(Guid organizationId)
        {
            try
            {
                var org = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (org == null)
                {
                    _logger.LogWarning("Organization {OrganizationId} not found for notification", organizationId);
                    return;
                }

                var user = await _userManager.FindByIdAsync(org.UserId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User not found for organization {OrganizationId}", organizationId);
                    return;
                }

                var adminEmails = _configuration.GetSection("AdminSettings:AdminEmails").Get<List<string>>()
                    ?? new List<string>
                    {
                        "admin1@smartmedi.com",
                        "admin2@smartmedi.com",
                        "admin3@smartmedi.com"
                    };

                foreach (var adminEmail in adminEmails)
                {
                    await _emailService.SendAdminOrganizationRegistrationNotificationAsync(
                        adminEmail,
                        org.Name,
                        org.Type.ToString(),
                        user.Email ?? string.Empty,
                        org.CreatedAt);
                }

                _logger.LogInformation(
                    "Admin notifications sent for organization {OrganizationId} registration",
                    organizationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin notifications for organization {OrganizationId}", organizationId);
            }
        }

        #endregion

        #region Organization Deletion

        /// <summary>
        /// Soft delete organization
        /// </summary>
        public async Task<Result> SoftDeleteOrganizationAsync(Guid organizationId, Guid adminUserId)
        {
            try
            {
                var org = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (org == null)
                    return Result.Failure("Organization not found");

                var user = await _userManager.FindByIdAsync(org.UserId.ToString());
                if (user == null)
                    return Result.Failure("Organization user not found");

                if (!user.IsActive)
                    return Result.Failure("Organization is already deleted");

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                try
                {
                    await _tokenService.RevokeAllUserRefreshTokensAsync(
                        org.UserId,
                        "Organization deleted by admin");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revoking tokens");
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Organization {OrgId} soft deleted by admin {AdminId}",
                    organizationId,
                    adminUserId);

                return Result.Success("Organization deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting organization");
                return Result.Failure("Failed to delete organization");
            }
        }

        /// <summary>
        /// Restore soft-deleted organization
        /// </summary>
        public async Task<Result> RestoreOrganizationAsync(Guid organizationId, Guid adminUserId)
        {
            try
            {
                var org = await _unitOfWork.Organizations.GetByIdAsync(organizationId);
                if (org == null)
                    return Result.Failure("Organization not found");

                var user = await _userManager.FindByIdAsync(org.UserId.ToString());
                if (user == null)
                    return Result.Failure("Organization user not found");

                if (user.IsActive)
                    return Result.Failure("Organization is already active");

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Organization {OrgId} restored by admin {AdminId}",
                    organizationId,
                    adminUserId);

                return Result.Success("Organization restored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring organization");
                return Result.Failure("Failed to restore organization");
            }
        }

        /// <summary>
        /// Get all soft-deleted organizations
        /// </summary>
        public async Task<Result<List<OrganizationListItemDto>>> GetDeletedOrganizationsAsync()
        {
            try
            {
                var allOrgs = await _unitOfWork.Organizations.GetAllAsync();
                var orgDtos = new List<OrganizationListItemDto>();

                foreach (var org in allOrgs)
                {
                    var user = await _userManager.FindByIdAsync(org.UserId.ToString());

                    if (user != null && !user.IsActive)
                    {
                        var documentCount = await _unitOfWork.OrganizationDocuments.CountAsync(
                            d => d.OrganizationId == org.Id);

                        orgDtos.Add(new OrganizationListItemDto
                        {
                            Id = org.Id,
                            UserId = org.UserId,
                            Name = org.Name,
                            Type = org.Type.ToString(),
                            ContactEmail = user.Email ?? string.Empty,
                            ContactPhone = user.PhoneNumber ?? string.Empty,
                            Address = org.Address,
                            City = org.City ?? string.Empty,
                            State = org.State ?? string.Empty,
                            ZipCode = org.ZipCode ?? string.Empty,
                            Country = org.Country ?? string.Empty,
                            VerificationStatus = org.VerificationStatus.ToString(),
                            RegisteredAt = org.CreatedAt,
                            VerifiedAt = org.VerifiedAt,
                            RejectionReason = org.RejectionReason,
                            DocumentCount = documentCount,
                            Website = org.Website,
                            SSN = org.SSN
                        });
                    }
                }

                return Result<List<OrganizationListItemDto>>.Success(
                    orgDtos.OrderByDescending(o => o.RegisteredAt).ToList(),
                    $"Retrieved {orgDtos.Count} deleted organizations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deleted organizations");
                return Result<List<OrganizationListItemDto>>.Failure("Failed to retrieve deleted organizations");
            }
        }

        #endregion

        #region Helper Methods

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        #endregion
    }
}