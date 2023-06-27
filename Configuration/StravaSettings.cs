namespace StrongEmailToStrava.Configuration;

public class StravaSettings
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string RefreshToken { get; set; }
    public required string GrantType { get; set; }
}
