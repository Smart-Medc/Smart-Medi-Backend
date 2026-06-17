using Microsoft.Extensions.Logging;
using Smart_Medc.Application.Common;
using Smart_Medc.Application.DTOs.Patient;
using Smart_Medc.Application.Interfaces;
using Smart_Medc.Domain.Entities.Identity;
using Smart_Medc.Domain.Enums;
using Smart_Medc.Domain.Interfaces.Repositories;

namespace Smart_Medc.Application.Services.Patient
{
    public class NotificationPreferencesService : INotificationPreferencesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationPreferencesService> _logger;

        public NotificationPreferencesService(
            IUnitOfWork unitOfWork,
            ILogger<NotificationPreferencesService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ServiceResult<NotificationPreferencesResponse>> GetAllPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return ServiceResult<NotificationPreferencesResponse>.NotFound("User not found");
                }

                var preferences = await _unitOfWork.UserNotificationPreferences
                    .GetByUserIdAsync(userId, cancellationToken);

                if (!preferences.Any())
                {
                    _logger.LogInformation("No preferences found for user {UserId}, initializing defaults", userId);
                    await InitializeDefaultPreferencesAsync(userId, cancellationToken);
                    preferences = await _unitOfWork.UserNotificationPreferences
                        .GetByUserIdAsync(userId, cancellationToken);
                }

                var response = new NotificationPreferencesResponse
                {
                    Preferences = preferences.Select(p => MapToDto(p)).ToList(),
                    AllEmailEnabled = preferences.All(p => p.EmailEnabled),
                    AllSmsEnabled = preferences.All(p => p.SmsEnabled),
                    AllPushEnabled = preferences.All(p => p.PushEnabled)
                };

                return ServiceResult<NotificationPreferencesResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification preferences for user {UserId}", userId);
                return ServiceResult<NotificationPreferencesResponse>.Failure("An error occurred while retrieving preferences");
            }
        }
        public async Task<ServiceResult<NotificationPreferenceDto>> GetPreferenceAsync(
            Guid userId,
            string notificationType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<NotificationType>(notificationType, ignoreCase: true, out var type))
                {
                    _logger.LogWarning("Invalid notification type: {NotificationType}", notificationType);
                    return ServiceResult<NotificationPreferenceDto>.Failure("Invalid notification type", 400);
                }

                var preference = await _unitOfWork.UserNotificationPreferences
                    .GetByUserAndTypeAsync(userId, (int)type, cancellationToken);

                if (preference == null)
                {
                    _logger.LogWarning("Preference not found for user {UserId}, type {NotificationType}", userId, type);
                    return ServiceResult<NotificationPreferenceDto>.NotFound("Preference not found");
                }

                return ServiceResult<NotificationPreferenceDto>.Success(MapToDto(preference));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving preference for user {UserId}", userId);
                return ServiceResult<NotificationPreferenceDto>.Failure("An error occurred while retrieving the preference");
            }
        }
        public async Task<ServiceResult<NotificationPreferenceDto>> UpdatePreferenceAsync(
            Guid userId,
            UpdateNotificationPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<NotificationType>(request.NotificationType, ignoreCase: true, out var type))
                {
                    _logger.LogWarning("Invalid notification type: {NotificationType}", request.NotificationType);
                    return ServiceResult<NotificationPreferenceDto>.Failure("Invalid notification type", 400);
                }

                var preference = await _unitOfWork.UserNotificationPreferences
                    .GetByUserAndTypeAsync(userId, (int)type, cancellationToken);

                if (preference == null)
                {
                    _logger.LogWarning("Preference not found for user {UserId}, type {Type}", userId, type);
                    return ServiceResult<NotificationPreferenceDto>.NotFound("Preference not found");
                }

                // Update preferences
                preference.EmailEnabled = request.EmailEnabled;
                preference.SmsEnabled = request.SmsEnabled;
                preference.PushEnabled = request.PushEnabled;
                preference.UpdatedAt = DateTime.UtcNow;


                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Notification preference updated for user {UserId}, type {Type}", userId, type);

                return ServiceResult<NotificationPreferenceDto>.Success(MapToDto(preference));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating preference for user {UserId}", userId);
                return ServiceResult<NotificationPreferenceDto>.Failure("An error occurred while updating the preference");
            }
        }
        public async Task<ServiceResult<NotificationPreferencesResponse>> UpdateAllPreferencesAsync(
            Guid userId,
            UpdateAllNotificationPreferencesRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var preferences = await _unitOfWork.UserNotificationPreferences
                    .GetByUserIdAsync(userId, cancellationToken);

                if (!preferences.Any())
                {
                    _logger.LogWarning("No preferences found for user {UserId}", userId);
                    return ServiceResult<NotificationPreferencesResponse>.NotFound("No preferences found");
                }


                foreach (var pref in preferences)
                {
                    pref.EmailEnabled = request.AllEmailEnabled;
                    pref.SmsEnabled = request.AllSmsEnabled;
                    pref.PushEnabled = request.AllPushEnabled;
                    pref.UpdatedAt = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("All notification preferences updated for user {UserId}", userId);

                var response = new NotificationPreferencesResponse
                {
                    Preferences = preferences.Select(p => MapToDto(p)).ToList(),
                    AllEmailEnabled = request.AllEmailEnabled,
                    AllSmsEnabled = request.AllSmsEnabled,
                    AllPushEnabled = request.AllPushEnabled
                };

                return ServiceResult<NotificationPreferencesResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all preferences for user {UserId}", userId);
                return ServiceResult<NotificationPreferencesResponse>.Failure("An error occurred while updating preferences");
            }
        }
        public async Task<ServiceResult> InitializeDefaultPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var existingPreferences = await _unitOfWork.UserNotificationPreferences
                    .GetByUserIdAsync(userId, cancellationToken);

                if (existingPreferences.Any())
                {
                    _logger.LogInformation("Preferences already exist for user {UserId}", userId);
                    return ServiceResult.Success();
                }

                var notificationTypes = Enum.GetValues(typeof(NotificationType))
                    .Cast<NotificationType>()
                    .ToList();

                foreach (var type in notificationTypes)
                {
                    var preference = new UserNotificationPreference
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        NotificationType = type,
                        EmailEnabled = true,
                        SmsEnabled = true,
                        PushEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.UserNotificationPreferences.AddAsync(preference, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Default notification preferences initialized for user {UserId}", userId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default preferences for user {UserId}", userId);
                return ServiceResult.Failure("An error occurred while initializing default preferences");
            }
        }




        private NotificationPreferenceDto MapToDto(UserNotificationPreference preference)
        {
            var (displayName, description) = GetNotificationTypeInfo(preference.NotificationType);

            return new NotificationPreferenceDto
            {
                Id = preference.Id,
                NotificationType = preference.NotificationType.ToString(),
                NotificationTypeDisplay = displayName,
                NotificationDescription = description,
                EmailEnabled = preference.EmailEnabled,
                SmsEnabled = preference.SmsEnabled,
                PushEnabled = preference.PushEnabled,
                CreatedAt = preference.CreatedAt,
                UpdatedAt = preference.UpdatedAt
            };
        }
        private (string DisplayName, string Description) GetNotificationTypeInfo(NotificationType type)
        {
            return type switch
            {
                NotificationType.AppointmentReminder =>
                    ("Appointment Reminders", "Get reminded about upcoming appointments"),

                NotificationType.MedicationReminder =>
                    ("Medication Reminders", "Get reminders to take your medications"),

                NotificationType.AppointmentRequest =>
                    ("Appointment Requests", "Receive new appointment requests from healthcare providers"),

                NotificationType.AppointmentConfirmation =>
                    ("Appointment Confirmations", "Receive confirmation of scheduled appointments"),

                NotificationType.AppointmentCancellation =>
                    ("Appointment Cancellations", "Receive notifications about cancelled appointments"),

                NotificationType.RecordAccess =>
                    ("Record Access Notifications", "Be notified when your medical records are accessed"),

                NotificationType.AIHealthAlert =>
                    ("AI Health Alerts", "Receive AI-powered health insights and alerts"),

                NotificationType.SystemNotification =>
                    ("System Notifications", "Receive important system and account notifications"),

                _ => ("Unknown", "Unknown notification type")
            };
        }
    }
}