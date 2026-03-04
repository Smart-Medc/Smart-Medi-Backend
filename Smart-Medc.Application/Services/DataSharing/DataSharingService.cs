using AutoMapper;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.DataSharing;
using Smart_Medc.Application.DTOs.Notifications;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Application.Interfaces.Notifications;
using Smart_Medc.Domain.Entities.DataSharing;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services.DataSharing
{
    public class DataSharingService : IDataSharingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly string _baseShareUrl; // Should be injected from configuration
        private readonly INotificationService _notificationService;

        public DataSharingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _baseShareUrl = "https://smartmedi.com/share";
        }

        public async Task<DataShareCodeDto> GenerateShareCodeAsync(
            Guid userId,
            GenerateShareCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            // Parse expiration type
            if (!Enum.TryParse<DataShareExpirationType>(dto.ExpirationType, true, out var expirationType))
                throw new ArgumentException("Invalid expiration type");

            // Validate all records belong to patient
            foreach (var recordId in dto.SpecificRecordIds)
            {
                var record = await _unitOfWork.MedicalRecords.GetByIdAsync(recordId, cancellationToken);
                if (record == null || record.PatientId != patient.Id || record.IsDeleted)
                    throw new UnauthorizedAccessException($"Medical record {recordId} not found or unauthorized");
            }

            // Generate unique code
            var codeString = await GenerateUniqueCodeAsync(cancellationToken);

            var entity = new DataShareCode
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Code = codeString,
                ShareUrl = $"{_baseShareUrl}/{codeString}",
                ExpirationType = expirationType,
                ExpiresAt = CalculateExpiration(expirationType),
                Status = DataShareStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DataShareCodes.AddAsync(entity, cancellationToken);

            // Link specific records
            foreach (var recordId in dto.SpecificRecordIds)
            {
                await _unitOfWork.DataShareRecordAccesses.AddAsync(new DataShareRecordAccess
                {
                    Id = Guid.NewGuid(),
                    DataShareCodeId = entity.Id,
                    MedicalRecordId = recordId,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Reload with navigation properties
            entity = await _unitOfWork.DataShareCodes.GetByCodeWithRecordsAsync(codeString, cancellationToken);

            return _mapper.Map<DataShareCodeDto>(entity);
        }

        public async Task<PagedResult<DataShareCodeDto>> GetCodesAsync(
            Guid userId,
            string filter = "active",
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            bool activeOnly = filter == "active";

            // Validate and sanitize pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 20;
            if (pageSize > 100)
                pageSize = 100; // Max page size

            // Get codes for the patient
            var codes = await _unitOfWork.DataShareCodes.GetCodesByPatientIdAsync(patient.Id, activeOnly, cancellationToken);

            // Order results (most recent first)
            var orderedCodes = codes
                .ToList();
            var totalCount = orderedCodes.Count;

            // Apply pagination
            var pagedCodes = orderedCodes
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var mappedCodes = _mapper.Map<List<DataShareCodeDto>>(pagedCodes);

            return new PagedResult<DataShareCodeDto>(
                mappedCodes,
                totalCount,
                pageNumber,
                pageSize
            );
        }

        public async Task RevokeCodeAsync(
            Guid userId,
            Guid codeId,
            CancellationToken cancellationToken = default)
        {
            // Validate patient exists
            var patient = await _unitOfWork.Patients.GetByUserIdAsync(userId, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            var code = await _unitOfWork.DataShareCodes.GetByIdAsync(codeId, cancellationToken);
            if (code == null)
                throw new KeyNotFoundException("Share code not found");

            // Authorization check
            if (code.PatientId != patient.Id)
                throw new UnauthorizedAccessException("You don't have permission to revoke this code");

            // Validate current status
            if (code.Status != DataShareStatus.Active)
                throw new InvalidOperationException("Code is not active");

            code.Status = DataShareStatus.Revoked;
            code.RevokedAt = DateTime.UtcNow;
            code.RevokedReason = "Revoked by patient";

            await _unitOfWork.DataShareCodes.UpdateAsync(code, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<ValidateCodeResponseDto> ValidateCodeAsync(
            string code,
            string? ipAddress = null,
            string? userAgent = null,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be null or empty", nameof(code));

            // Sanitize code (trim whitespace, convert to uppercase for case-insensitive comparison)
            code = code.Trim().ToUpper();

            // Validate code format and length (prevent SQL injection, ensure reasonable format)
            if (code.Length < 3 || code.Length > 50)
                throw new ArgumentException("Invalid code format", nameof(code));

            // Basic format validation (alphanumeric and hyphens only)
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9\-]+$"))
                throw new ArgumentException("Code contains invalid characters", nameof(code));

            // Validate organizationId if provided
            Domain.Entities.OrganizationModels.Organization? organization = null;
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                organization = await _unitOfWork.Organizations.GetByUserIdAsync(userId.Value, cancellationToken);
                if (organization == null)
                    throw new KeyNotFoundException("Organization not found");
            }

            // Validate IP address length (prevent malicious inputs)
            if (!string.IsNullOrWhiteSpace(ipAddress) && ipAddress.Length > 45) // IPv6 max length
                ipAddress = ipAddress.Substring(0, 45);

            // Validate User-Agent length (prevent extremely long strings)
            if (!string.IsNullOrWhiteSpace(userAgent) && userAgent.Length > 500)
                userAgent = userAgent.Substring(0, 500);

            // Get code entity
            var entity = await _unitOfWork.DataShareCodes.GetByCodeAsync(code, cancellationToken);

            if (entity == null)
                return new ValidateCodeResponseDto { IsValid = false, Message = "Invalid code" };

            // Check status
            if (entity.Status == DataShareStatus.Revoked)
                return new ValidateCodeResponseDto { IsValid = false, Message = "Code has been revoked" };

            if (entity.Status == DataShareStatus.Expired)
                return new ValidateCodeResponseDto { IsValid = false, Message = "Code has expired" };

            if (entity.Status == DataShareStatus.MaxAccessReached)
                return new ValidateCodeResponseDto { IsValid = false, Message = "Maximum access limit reached" };

            //  Check expiration
            if (entity.ExpiresAt.HasValue && entity.ExpiresAt < DateTime.UtcNow)
            {
                // Auto-expire the code
                entity.Status = DataShareStatus.Expired;
                await _unitOfWork.DataShareCodes.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return new ValidateCodeResponseDto { IsValid = false, Message = "Code has expired" };
            }

            // Check max access count
            if (entity.MaxAccessCount.HasValue)
            {
                var accessCount = await _unitOfWork.DataShareAccessLogs
                    .CountAsync(l => l.DataShareCodeId == entity.Id, cancellationToken);

                if (accessCount >= entity.MaxAccessCount.Value)
                {
                    // Update status to MaxAccessReached
                    entity.Status = DataShareStatus.MaxAccessReached;
                    await _unitOfWork.DataShareCodes.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return new ValidateCodeResponseDto { IsValid = false, Message = "Maximum access limit reached" };
                }
            }

            // Log access (with validated/sanitized inputs)
            var accessLog = new DataShareAccessLog
            {
                Id = Guid.NewGuid(),
                DataShareCodeId = entity.Id,
                OrganizationId = organization != null ? organization.Id : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AccessedAt = DateTime.UtcNow
            };

            await _unitOfWork.DataShareAccessLogs.AddAsync(accessLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notification to patient about access
            await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
            {
                PatientId = entity.PatientId,
                Type = PatientNotificationType.RecordAccess,
                Priority = NotificationPriority.Normal,
                Title = "Your Medical Records Were Accessed",
                Message = organization != null
                    ? $"{organization.Name} accessed your shared medical records."
                    : "Someone accessed your shared medical records.",
                ActionUrl = "/data-sharing",
                Data = $"{{\"shareCodeId\":\"{entity.Id}\",\"code\":\"{entity.Code}\"}}"
            }, cancellationToken);

            // Calculate remaining time
            var remaining = entity.ExpiresAt.HasValue
                ? (int)(entity.ExpiresAt.Value - DateTime.UtcNow).TotalSeconds
                : int.MaxValue;

            // Ensure remaining time is not negative
            if (remaining < 0)
                remaining = 0;

            // Return success response
            return new ValidateCodeResponseDto
            {
                IsValid = true,
                Message = "Code is valid",
                RemainingTimeSeconds = remaining,
                PatientName = entity.Patient?.User?.FullName ?? "Unknown"
            };
        }

        public async Task<PagedResult<SharedMedicalRecordDto>> GetSharedRecordsAsync(
            string code,
            string? ipAddress = null,
            string? userAgent = null,
            Guid? userId = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // Validate and sanitize input parameters
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be null or empty", nameof(code));

            // Sanitize code (trim whitespace, convert to uppercase)
            code = code.Trim().ToUpper();

            // Validate code format and length
            if (code.Length < 3 || code.Length > 50)
                throw new ArgumentException("Invalid code format", nameof(code));

            // Basic format validation (alphanumeric and hyphens only)
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9\-]+$"))
                throw new ArgumentException("Code contains invalid characters", nameof(code));

            // Validate pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 20;
            if (pageSize > 100)
                pageSize = 100; // Max page size

            // Validate organizationId if provided
            Domain.Entities.OrganizationModels.Organization? organization = null;
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                organization = await _unitOfWork.Organizations.GetByIdAsync(userId.Value, cancellationToken);
                if (organization == null)
                    throw new KeyNotFoundException("Organization not found");
            }

            // Sanitize audit fields
            if (!string.IsNullOrWhiteSpace(ipAddress) && ipAddress.Length > 45)
                ipAddress = ipAddress.Substring(0, 45);
            if (!string.IsNullOrWhiteSpace(userAgent) && userAgent.Length > 500)
                userAgent = userAgent.Substring(0, 500);

            // Get code entity with related data
            var entity = await _unitOfWork.DataShareCodes.GetByCodeWithRecordsAsync(code, cancellationToken);
            if (entity == null)
                throw new UnauthorizedAccessException("Invalid code");

            // Check if code status is already marked (fast path)
            if (entity.Status == DataShareStatus.Revoked)
                throw new UnauthorizedAccessException("Code has been revoked");

            if (entity.Status == DataShareStatus.Expired)
                throw new UnauthorizedAccessException("Code has expired");

            if (entity.Status == DataShareStatus.MaxAccessReached)
                throw new UnauthorizedAccessException("Maximum access limit reached");

            // Check expiration (slow path - calculate and update if needed)
            if (entity.ExpiresAt.HasValue && entity.ExpiresAt < DateTime.UtcNow)
            {
                // Auto-expire the code
                entity.Status = DataShareStatus.Expired;
                await _unitOfWork.DataShareCodes.UpdateAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedAccessException("Code has expired");
            }

            // Check max access count (slow path - calculate and update if needed)
            if (entity.MaxAccessCount.HasValue)
            {
                var accessCount = await _unitOfWork.DataShareAccessLogs
                    .CountAsync(l => l.DataShareCodeId == entity.Id, cancellationToken);

                if (accessCount >= entity.MaxAccessCount.Value)
                {
                    // Update status to MaxAccessReached
                    entity.Status = DataShareStatus.MaxAccessReached;
                    await _unitOfWork.DataShareCodes.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    throw new UnauthorizedAccessException("Maximum access limit reached");
                }
            }

            // Log access (with validated/sanitized inputs)
            var accessLog = new DataShareAccessLog
            {
                Id = Guid.NewGuid(),
                DataShareCodeId = entity.Id,
                OrganizationId = organization != null ? organization.Id : null,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AccessedAt = DateTime.UtcNow
            };

            await _unitOfWork.DataShareAccessLogs.AddAsync(accessLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notification to patient about access
            await _notificationService.SendToPatientAsync(new CreatePatientNotificationDto
            {
                PatientId = entity.PatientId,
                Type = PatientNotificationType.RecordAccess,
                Priority = NotificationPriority.Normal,
                Title = "Your Medical Records Were Viewed",
                Message = organization != null
                ? $"{organization.Name} viewed your shared medical records."
                : "Someone viewed your shared medical records.",
                ActionUrl = "/data-sharing",
                Data = $"{{\"shareCodeId\":\"{entity.Id}\",\"code\":\"{entity.Code}\"}}"
            }, cancellationToken);

            // Get and filter records
            var records = entity.RecordAccesses
                .Select(ra => ra.MedicalRecord)
                .Where(mr => mr != null && !mr.IsDeleted) // Ensure not null and not deleted
                .OrderByDescending(mr => mr.CreatedAt) // Most recent first
                .ToList();

            // Apply pagination
            var totalCount = records.Count;
            var pagedRecords = records
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to DTOs
            var mappedRecords = _mapper.Map<List<SharedMedicalRecordDto>>(pagedRecords);

            // Return paged result
            return new PagedResult<SharedMedicalRecordDto>(
                mappedRecords,
                totalCount,
                pageNumber,
                pageSize
            );
        }

        private DateTime? CalculateExpiration(DataShareExpirationType type)
        {
            return type switch
            {
                DataShareExpirationType.OneHour => DateTime.UtcNow.AddHours(1),
                DataShareExpirationType.SixHours => DateTime.UtcNow.AddHours(6),
                DataShareExpirationType.TwentyFourHours => DateTime.UtcNow.AddHours(24),
                DataShareExpirationType.SevenDays => DateTime.UtcNow.AddDays(7),
                DataShareExpirationType.ThirtyDays => DateTime.UtcNow.AddDays(30),
                DataShareExpirationType.Permanent => null,
                _ => DateTime.UtcNow.AddHours(24) // Default fallback
            };
        }

        private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
        {
            string code;
            bool isUnique;

            do
            {
                // Format: MED-XXXXX (5 alphanumeric characters)
                var randomPart = Guid.NewGuid().ToString("N")[..5].ToUpper();
                code = $"MED-{randomPart}";

                isUnique = await _unitOfWork.DataShareCodes.IsCodeUniqueAsync(code, cancellationToken);
            } while (!isUnique);

            return code;
        }
    }
}
