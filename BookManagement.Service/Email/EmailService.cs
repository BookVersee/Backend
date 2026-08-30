using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookManagement.Service.Email
{
    /// Vị trí: Infrastructure Service - Tích hợp dịch vụ gửi Email thông báo qua SMTP Gmail.
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        /// Chức năng: Gửi thư điện tử Email nội dung HTML qua SMTP Server
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_emailOptions.SenderEmail) || string.IsNullOrWhiteSpace(_emailOptions.AppPassword))
            {
                _logger.LogWarning("[SMTP Gmail] AppPassword or SenderEmail is unconfigured in appsettings.json. Simulated email sent to {ToEmail}", toEmail);
                return;
            }

            try
            {
                var sanitizedPassword = _emailOptions.AppPassword.Replace(" ", "").Trim();

                using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailOptions.SenderEmail, sanitizedPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailOptions.SenderEmail, _emailOptions.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("[SMTP Gmail] Email successfully sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMTP Gmail Failure] Error sending email to {ToEmail}: {Message}", toEmail, ex.Message);
            }
        }
    }
}
