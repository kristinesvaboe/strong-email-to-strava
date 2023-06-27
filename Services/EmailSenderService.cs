using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using StrongEmailToStrava.Configuration;

namespace StrongEmailToStrava.Services;

public interface IEmailSenderService
{
    Task SendEmailAsync(string subject, string body);
}

public class EmailSenderService : IEmailSenderService
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private SmtpSettings _settings;

    public EmailSenderService(IConfiguration config, ILogger<EmailSenderService> logger)
    {
        _config = config;
        _logger = logger;
        _settings = _config.GetSection("SmtpSettings").Get<SmtpSettings>()!;
    }

    public async Task SendEmailAsync(string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.Sender));
        email.To.Add(MailboxAddress.Parse(_settings.Receiver));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Plain) { Text = body };


        using (var client = new SmtpClient())
        {
            try
            {
                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
            }
            catch (SmtpCommandException e)
            {
                _logger.LogError("Error trying to connect: {0}", e.Message);
                _logger.LogError("\tStatusCode: {0}", e.StatusCode);
                return;
            }
            catch (SmtpProtocolException e)
            {
                _logger.LogError("Protocol error while trying to connect: {0}", e.Message);
                return;
            }

            try
            {
                await client.AuthenticateAsync(_settings.Sender, _settings.Password);
            }
            catch (AuthenticationException e)
            {
                _logger.LogError("Invalid credentials: {0}", e.Message);
                return;
            }
            catch (SmtpCommandException e)
            {
                _logger.LogError("Error trying to authenticate: {0}", e.Message);
                _logger.LogError("\tStatuscode: {0}", e.StatusCode);
                return;
            }
            catch (SmtpProtocolException e)
            {
                _logger.LogError("Protocol error while trying to authenticate: {0}", e.Message);
                return;
            }

            try
            {
                await client.SendAsync(email);
            }
            catch (SmtpCommandException e)
            {
                _logger.LogError("Error sending message: {0}", e.Message);
                _logger.LogError("\tStatusCode: {0}", e.StatusCode);

                switch (e.ErrorCode)
                {
                    case SmtpErrorCode.RecipientNotAccepted:
                        _logger.LogError("\tRecipient not accepted: {0}", e.Mailbox);
                        break;
                    case SmtpErrorCode.SenderNotAccepted:
                        _logger.LogError("\tSender not accepted: {0}", e.Mailbox);
                        break;
                    case SmtpErrorCode.MessageNotAccepted:
                        _logger.LogError("\tMessage not accepted.");
                        break;
                }
            }
            catch (SmtpProtocolException e)
            {
                Console.WriteLine("Protocol error while sending message: {0}", e.Message);
            }

            await client.DisconnectAsync(true);
        }
    }

}