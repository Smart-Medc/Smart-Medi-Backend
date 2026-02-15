namespace Smart_Medc.Application.Interfaces.Auth
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendOtpEmailAsync(string to, string otpCode, string firstName);
        Task<bool> SendWelcomeEmailAsync(string to, string firstName);
        Task<bool> SendPasswordResetOtpEmailAsync(string to, string otpCode, string firstName);

        Task<bool> SendAdminOrganizationRegistrationNotificationAsync(
            string adminEmail,
            string organizationName,
            string organizationType,
            string organizationEmail,
            DateTime registrationDate);

        Task<bool> SendOrganizationApprovedEmailAsync(
            string to,
            string organizationName,
            string contactPerson);

        Task<bool> SendOrganizationRejectedEmailAsync(
            string to,
            string organizationName,
            string contactPerson,
            string rejectionReason);
    }
}