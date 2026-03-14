using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smart_Medc.Application.Configuration;
using Smart_Medc.Application.Interfaces.Auth;

namespace Smart_Medc.Application.Services.Auth
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private const int OTP_EXPIRATION_MINUTES = 5;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    EnableSsl = _emailSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                return false;
            }
        }

        public async Task<bool> SendOtpEmailAsync(string to, string otpCode, string firstName)
        {
            var currentYear = DateTime.UtcNow.Year;
            var privacyPolicyLink = "https://smartmedi.com/privacy"; // TODO: Update with actual URL
            var termsLink = "https://smartmedi.com/terms"; // TODO: Update with actual URL

            var subject = "Verify Your Email - Smart Medi";
            var body = $@"
               <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <style>
                        body {{ 
                            font-family: Arial, sans-serif; 
                            line-height: 1.6; 
                            color: #333; 
                            margin: 0;
                            padding: 0;
                            background-color: #f4f4f4;
                        }}
                        .container {{ 
                            max-width: 600px; 
                            margin: 20px auto; 
                            background-color: white;
                            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                        }}
                        .header {{ 
                            background-color: #4F46E5; 
                            color: white; 
                            padding: 30px 20px; 
                            text-align: center; 
                            border-radius: 5px 5px 0 0; 
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 28px;
                        }}
                        .content {{ 
                            background-color: #ffffff; 
                            padding: 40px 30px; 
                        }}
                        .otp-code {{ 
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white; 
                            font-size: 36px; 
                            font-weight: bold; 
                            padding: 20px; 
                            text-align: center; 
                            letter-spacing: 8px; 
                            margin: 30px 0; 
                            border-radius: 8px; 
                            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                        }}
                        .expiry {{
                            text-align: center;
                            color: #666;
                            font-size: 14px;
                            margin-bottom: 20px;
                        }}
                        .warning {{ 
                            background-color: #FEF2F2; 
                            border-left: 4px solid #EF4444; 
                            padding: 15px; 
                            margin: 25px 0; 
                            border-radius: 4px;
                        }}
                        .warning strong {{
                            color: #DC2626;
                        }}
                        .button {{
                            display: inline-block;
                            background-color: #4F46E5;
                            color: white;
                            padding: 12px 30px;
                            text-decoration: none;
                            border-radius: 5px;
                            margin: 20px 0;
                            font-weight: bold;
                        }}
                        .footer {{ 
                            text-align: center; 
                            padding: 20px; 
                            font-size: 12px; 
                            color: #666; 
                            background-color: #f9f9f9;
                            border-radius: 0 0 5px 5px;
                        }}
                        .footer p {{
                            margin: 5px 0;
                        }}
        
                        /* Mobile responsive */
                        @media only screen and (max-width: 600px) {{
                            .container {{
                                margin: 10px;
                            }}
                            .content {{
                                padding: 20px 15px;
                            }}
                            .otp-code {{
                                font-size: 28px;
                                letter-spacing: 5px;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>🏥 Smart Medi</h1>
                        </div>
                        <div class=""content"">
                            <h2>Hello {firstName}!</h2>
                            <p>Thank you for registering with Smart Medi. To complete your registration, please verify your email address using the OTP code below:</p>
            
                            <div class=""otp-code"">{otpCode}</div>
            
                            <p class=""expiry"">⏱️ This code will expire in <strong>{OTP_EXPIRATION_MINUTES} minutes</strong></p>
            
                            <div class=""warning"">
                                <strong>⚠️ Security Notice:</strong> If you didn't request this code, please ignore this email. Never share your OTP code with anyone.
                            </div>
            
                            <p style=""margin-top: 30px;"">If you have any questions, feel free to contact our support team at <a href=""mailto:support@smartmedi.com"">support@smartmedi.com</a>.</p>
            
                            <p style=""margin-top: 20px;"">Best regards,<br><strong>The Smart Medi Team</strong></p>
                        </div>
                        <div class=""footer"">
                            <p>&copy; {currentYear} Smart Medi. All rights reserved.</p>
                            <p>This is an automated message, please do not reply to this email.</p>
                            <p><a href=""{privacyPolicyLink}"" style=""color: #4F46E5;"">Privacy Policy</a> | <a href=""{termsLink}"" style=""color: #4F46E5;"">Terms of Service</a></p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string firstName)
        {
            var subject = "Welcome to Smart Medi!";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; 
                                  border-radius: 5px; display: inline-block; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to Smart Medi!</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {firstName}!</h2>
                            <p>Your email has been verified successfully. Welcome to Smart Medi - your comprehensive medical management platform!</p>
                            
                            <p>You can now access all features including:</p>
                            <ul>
                                <li>📋 Medical Records Management</li>
                                <li>💊 Medication Tracking</li>
                                <li>📅 Appointment Scheduling</li>
                                <li>🤖 AI Health Assistant</li>
                                <li>📝 Health Journal</li>
                            </ul>
                            
                            <p>Get started by completing your profile and exploring the platform.</p>
                            
                            <p>Best regards,<br>The Smart Medi Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; {DateTime.UtcNow.Year} Smart Medi. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }

        //public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink, string firstName)
        //{
        //    var subject = "Reset Your Password - Smart Medi";
        //    var body = $@"
        //        <!DOCTYPE html>
        //        <html>
        //        <head>
        //            <style>
        //                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        //                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        //                .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        //                .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        //                .button {{ background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; 
        //                          border-radius: 5px; display: inline-block; margin: 20px 0; }}
        //                .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        //                .warning {{ background-color: #FEF2F2; border-left: 4px solid #EF4444; padding: 10px; margin: 20px 0; }}
        //            </style>
        //        </head>
        //        <body>
        //            <div class='container'>
        //                <div class='header'>
        //                    <h1>Password Reset Request</h1>
        //                </div>
        //                <div class='content'>
        //                    <h2>Hello {firstName}!</h2>
        //                    <p>We received a request to reset your password. Click the button below to reset it:</p>

        //                    <a href='{resetLink}' class='button'>Reset Password</a>

        //                    <p>This link will expire in <strong>1 hour</strong>.</p>

        //                    <div class='warning'>
        //                        <strong>⚠️ Security Notice:</strong> If you didn't request this password reset, please ignore this email and ensure your account is secure.
        //                    </div>

        //                    <p>Best regards,<br>The Smart Medi Team</p>
        //                </div>
        //                <div class='footer'>
        //                    <p>&copy; {DateTime.UtcNow.Year} Smart Medi. All rights reserved.</p>
        //                </div>
        //            </div>
        //        </body>
        //        </html>
        //    ";

        //    return await SendEmailAsync(to, subject, body, true);
        //}

        public async Task<bool> SendPasswordResetOtpEmailAsync(string to, string otpCode, string firstName)
        {
            var subject = "Reset Your Password - Smart Medi";
            var currentYear = DateTime.UtcNow.Year;
            var supportEmail = "support@smartmedi.com";

            var body = $@"
                   <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset=""UTF-8"">
                        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                        <style>
                            body {{ 
                                font-family: Arial, sans-serif; 
                                line-height: 1.6; 
                                color: #333; 
                                margin: 0;
                                padding: 0;
                                background-color: #f4f4f4;
                            }}
                            .container {{ 
                                max-width: 600px; 
                                margin: 20px auto; 
                                background-color: white;
                                box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                            }}
                            .header {{ 
                                background-color: #DC2626; 
                                color: white; 
                                padding: 30px 20px; 
                                text-align: center; 
                                border-radius: 5px 5px 0 0; 
                            }}
                            .header h1 {{
                                margin: 0;
                                font-size: 28px;
                            }}
                            .content {{ 
                                background-color: #ffffff; 
                                padding: 40px 30px; 
                            }}
                            .otp-code {{ 
                                background: linear-gradient(135deg, #DC2626 0%, #991B1B 100%);
                                color: white; 
                                font-size: 36px; 
                                font-weight: bold; 
                                padding: 20px; 
                                text-align: center; 
                                letter-spacing: 8px; 
                                margin: 30px 0; 
                                border-radius: 8px; 
                                box-shadow: 0 4px 6px rgba(0,0,0,0.1);
                            }}
                            .expiry {{
                                text-align: center;
                                color: #666;
                                font-size: 14px;
                                margin-bottom: 20px;
                            }}
                            .warning {{ 
                                background-color: #FEF2F2; 
                                border-left: 4px solid #EF4444; 
                                padding: 15px; 
                                margin: 25px 0; 
                                border-radius: 4px;
                            }}
                            .warning strong {{
                                color: #DC2626;
                            }}
                            .footer {{ 
                                text-align: center; 
                                padding: 20px; 
                                font-size: 12px; 
                                color: #666; 
                                background-color: #f9f9f9;
                                border-radius: 0 0 5px 5px;
                            }}
                            .footer p {{
                                margin: 5px 0;
                            }}
                            .footer a {{
                                color: #DC2626;
                                text-decoration: none;
                            }}

                            /* Mobile responsive */
                            @media only screen and (max-width: 600px) {{
                                .container {{
                                    margin: 10px;
                                }}
                                .content {{
                                    padding: 20px 15px;
                                }}
                                .otp-code {{
                                    font-size: 28px;
                                    letter-spacing: 5px;
                                }}
                            }}
                        </style>
                    </head>
                    <body>
                        <div class=""container"">
                            <div class=""header"">
                                <h1>🔒 Password Reset Request</h1>
                            </div>
                            <div class=""content"">
                                <h2>Hello {firstName}!</h2>
                                <p>We received a request to reset your password. Use the verification code below to proceed:</p>
    
                                <div class=""otp-code"">{otpCode}</div>
    
                                <p class=""expiry"">⏱️ This code will expire in <strong>{OTP_EXPIRATION_MINUTES}</strong></p>
    
                                <div class=""warning"">
                                    <strong>⚠️ Security Notice:</strong> If you didn't request this password reset, please ignore this email and ensure your account is secure. Your password will not be changed unless you use this code.
                                </div>
    
                                <p style=""margin-top: 30px;"">If you need assistance, contact us at <a href=""mailto:{supportEmail}"">{supportEmail}</a>.</p>
    
                                <p style=""margin-top: 20px;"">Best regards,<br><strong>The Smart Medi Team</strong></p>
                            </div>
                            <div class=""footer"">
                                <p>&copy; {currentYear} Smart Medi. All rights reserved.</p>
                                <p>This is an automated message, please do not reply to this email.</p>
                            </div>
                        </div>
                    </body>
                    </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }

        public async Task<bool> SendAdminOrganizationRegistrationNotificationAsync(
                string adminEmail,
                string organizationName,
                string organizationType,
                string organizationEmail,
                DateTime registrationDate)
        {
            var subject = "🏥 New Organization Registration - Action Required";
            var dashboardUrl = "https://admin-dashboard-sigma-one-74.vercel.app/";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 20px auto; background: white; }}
                        .header {{ background: #3B82F6; color: white; padding: 30px; text-align: center; }}
                        .content {{ padding: 30px; }}
                        .info-box {{ background: #F3F4F6; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                        .button {{ 
                            display: inline-block; 
                            background: #3B82F6; 
                            color: white; 
                            padding: 12px 30px; 
                            text-decoration: none; 
                            border-radius: 6px; 
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>🏥 New Organization Registration</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello Admin,</p>
                            <p>A new organization has registered and requires verification.</p>
                    
                            <div class=""info-box"">
                                <p><strong>Organization:</strong> {organizationName}</p>
                                <p><strong>Type:</strong> {organizationType}</p>
                                <p><strong>Email:</strong> {organizationEmail}</p>
                                <p><strong>Registered:</strong> {registrationDate:MMMM dd, yyyy HH:mm}</p>
                            </div>
                    
                            <p><strong>Action Required:</strong> Review documents and approve/reject verification.</p>
                    
                            <div style=""text-align: center;"">
                                <a href=""{dashboardUrl}"" class=""button"">Go to Admin Dashboard →</a>
                            </div>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(adminEmail, subject, body, true);
        }

        public async Task<bool> SendOrganizationApprovedEmailAsync(
            string to,
            string organizationName,
            string contactPerson)
        {
            var subject = "✅ Organization Verified - Smart Medi";
            var loginUrl = "https://smartmedi.com/login";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 20px auto; background: white; }}
                        .header {{ background: #10B981; color: white; padding: 30px; text-align: center; }}
                        .content {{ padding: 30px; }}
                        .button {{ 
                            display: inline-block; 
                            background: #10B981; 
                            color: white; 
                            padding: 12px 30px; 
                            text-decoration: none; 
                            border-radius: 6px; 
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>✅ Congratulations!</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {contactPerson},</p>
                            <p>Great news! <strong>{organizationName}</strong> has been verified and approved.</p>
                            <p>You can now access all platform features.</p>
                    
                            <div style=""text-align: center; margin: 30px 0;"">
                                <a href=""{loginUrl}"" class=""button"">Login to Dashboard →</a>
                            </div>
                    
                            <p>Best regards,<br><strong>Smart Medi Team</strong></p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }

        public async Task<bool> SendOrganizationRejectedEmailAsync(
            string to,
            string organizationName,
            string contactPerson,
            string rejectionReason)
        {
            var subject = "Organization Verification - Additional Information Needed";
            var contactUrl = "https://smartmedi.com/contact";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 20px auto; background: white; }}
                        .header {{ background: #EF4444; color: white; padding: 30px; text-align: center; }}
                        .content {{ padding: 30px; }}
                        .reason-box {{ 
                            background: #FEF2F2; 
                            border-left: 4px solid #EF4444; 
                            padding: 20px; 
                            margin: 20px 0; 
                        }}
                        .button {{ 
                            display: inline-block; 
                            background: #3B82F6; 
                            color: white; 
                            padding: 12px 30px; 
                            text-decoration: none; 
                            border-radius: 6px; 
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>Verification Status Update</h1>
                        </div>
                        <div class=""content"">
                            <p>Hello {contactPerson},</p>
                            <p>Thank you for registering <strong>{organizationName}</strong>. We need additional information before proceeding.</p>
                    
                            <div class=""reason-box"">
                                <strong>Reason:</strong><br>{rejectionReason}
                            </div>
                    
                            <p>Please review the feedback and contact support for clarification.</p>
                    
                            <div style=""text-align: center; margin: 30px 0;"">
                                <a href=""{contactUrl}"" class=""button"">Contact Support →</a>
                            </div>
                    
                            <p>Best regards,<br><strong>Smart Medi Team</strong></p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            return await SendEmailAsync(to, subject, body, true);
        }
    }
}