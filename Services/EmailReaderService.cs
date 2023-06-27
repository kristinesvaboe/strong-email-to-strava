using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using StrongEmailToStrava.Configuration;

namespace StrongEmailToStrava.Services;

public interface IEmailReaderService
{
    MimeMessage GetEmail(UniqueId uid);
    IList<UniqueId> GetEmails();
    void MarkEmailAsRead(UniqueId uid);
}

public class EmailReaderService : IEmailReaderService, IDisposable
{
    private readonly ImapClient _client = new ImapClient();
    private readonly IConfiguration _config;
    private readonly ILogger<EmailReaderService> _logger;
    private ImapSettings _settings;

    public EmailReaderService(IConfiguration config, ILogger<EmailReaderService> logger)
    {
        _config = config;
        _logger = logger;
        _settings = _config.GetSection("ImapSettings").Get<ImapSettings>()!;
    }

    IMailFolder GetInbox()
    {
        try
        {
            _client.Connect(_settings.ImapHost, _settings.ImapPort, true);
        }
        catch (ImapCommandException e)
        {
            _logger.LogError("Error trying to connect: {0}", e.Message);
            throw e;
        }
        catch (ImapProtocolException e)
        {
            _logger.LogError("Protocol error while trying to connect: {0}", e.Message);
            throw e;
        }

        try
        {
            _client.Authenticate(_settings.UserName, _settings.Password);
        }
        catch (ImapCommandException e)
        {
            _logger.LogError("Error trying to authenticate: {0}", e.Message);
            throw e;
        }
        catch (ImapProtocolException e)
        {
            _logger.LogError("Protocol error while trying to authenticate: {0}", e.Message);
            throw e;
        }

        var inbox = _client.Inbox;

        return inbox;
    }
    public IList<UniqueId> GetEmails()
    {
        var inbox = GetInbox();
        inbox.Open(FolderAccess.ReadOnly);

        // Get unread emails containing strong app link
        IList<UniqueId> unreadStrongEmails = inbox.Search(SearchQuery.BodyContains("https://strong.app.link/").And(SearchQuery.NotSeen));

        _client.Disconnect(true);

        _logger.LogInformation("{0} new workout email(s) found", unreadStrongEmails.Count);

        return unreadStrongEmails;
    }

    public MimeMessage GetEmail(UniqueId uid)
    {
        var inbox = GetInbox();
        inbox.Open(FolderAccess.ReadOnly);

        var message = inbox.GetMessage(uid);

        _client.Disconnect(true);

        return message;
    }

    public void MarkEmailAsRead(UniqueId uid)
    {
        using (_client)
        {
            var inbox = GetInbox();
            inbox.Open(FolderAccess.ReadWrite);

            inbox.AddFlags(uid, MessageFlags.Seen, true);

            _client.Disconnect(true);

            _logger.LogInformation("Email with uid {0} was marked as read.", uid);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
