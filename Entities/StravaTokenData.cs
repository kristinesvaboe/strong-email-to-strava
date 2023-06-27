using Newtonsoft.Json;

namespace StrongEmailToStrava.Entities;

public class StravaTokenData
{
    [JsonProperty("access_token")]
    public string AccessToken { get; set; } = null!;

    [JsonProperty("expires_at")]
    public int TokenExpiry { get; set; }

    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = null!;
}
