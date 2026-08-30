using System;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookManagement.Service.Email
{
    /// Vị trí: Infrastructure Service - Tích hợp dịch vụ gửi Email thông báo qua Google Cloud Console (Gmail API v1 / OAuth2).
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;
        }

        /// Chức năng: Gửi thư điện tử Email nội dung HTML qua Google Cloud Console (Gmail API v1 / OAuth2)
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_emailOptions.ClientId) ||
                string.IsNullOrWhiteSpace(_emailOptions.ClientSecret) ||
                string.IsNullOrWhiteSpace(_emailOptions.RefreshToken))
            {
                _logger.LogWarning("[Google Cloud Gmail API] ClientId, ClientSecret or RefreshToken is unconfigured in appsettings.json. Simulated email to {ToEmail}", toEmail);
                return;
            }

            try
            {
                var tokenResponse = new TokenResponse
                {
                    RefreshToken = _emailOptions.RefreshToken
                };

                var credential = new UserCredential(
                    new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = _emailOptions.ClientId,
                            ClientSecret = _emailOptions.ClientSecret
                        },
                        Scopes = new[] { GmailService.Scope.GmailSend }
                    }),
                    "user",
                    tokenResponse
                );

                var gmailService = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "BookManagementSystem"
                });

                var rawMimeMessage = $"From: {_emailOptions.SenderName} <{_emailOptions.SenderEmail}>\r\n" +
                                     $"To: {toEmail}\r\n" +
                                     $"Subject: =?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(subject))}?=\r\n" +
                                     "Content-Type: text/html; charset=utf-8\r\n\r\n" +
                                     htmlBody;

                var base64UrlRaw = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawMimeMessage))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                var gmailMessage = new Message
                {
                    Raw = base64UrlRaw
                };

                await gmailService.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
                _logger.LogInformation("[Google Cloud Gmail API] Email successfully sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Google Cloud Gmail API Failure] Error sending email via Gmail API to {ToEmail}: {Message}", toEmail, ex.Message);
            }
        }
    }
}
