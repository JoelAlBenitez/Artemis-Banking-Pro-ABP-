using Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives;
using Artemis_Banking_Pro.Core.Application.DTOs.Messages;
using ArtemisBankingPro.Core.Domain.Settings.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace ArtemisBankingPro.Infraestructrue.Shared.Services.Email
{
    public sealed class EmailServices : IEmailServices
    {

        private readonly EmailSettings _settings;
        private readonly ILogger<EmailServices> _logger;

        public EmailServices(IOptions<EmailSettings> settings,
            ILogger<EmailServices> logger
            )
        {
            _settings = settings.Value;
            _logger = logger;
        }
        public async Task<bool> SendNotification(MessageDto message)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", message.To));

            emailMessage.Subject = message.Subject;
            var body = new BodyBuilder { HtmlBody = message.Message};
            emailMessage.Body = body.ToMessageBody();


            using var client = new SmtpClient();
            try
            {

                var secure = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await client.ConnectAsync(_settings.SmtpServer, _settings.Port, secure);
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
                await client.SendAsync(emailMessage);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error ocurrido en {ex.Message}");
                return false;

            }
            finally
            {
                await client.DisconnectAsync(true);
            }


        }
    }
}
