namespace StrongEmailToStrava.Configuration;

public class SmtpSettings
{
    public required string SmtpServer { get; set; }
    public required int SmtpPort { get; set; }
    public required string SenderName { get; set; }
    public required string Receiver { get; set; }
    public required string Sender { get; set; }
    public required string Password { get; set; }
}
