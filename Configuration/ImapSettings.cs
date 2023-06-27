namespace StrongEmailToStrava.Configuration;

public class ImapSettings
{
    public required string MailFolderName { get; set; }
    public required string ImapHost { get; set; }
    public required int ImapPort { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
}

