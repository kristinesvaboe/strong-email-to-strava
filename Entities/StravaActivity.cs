using Newtonsoft.Json;

namespace StrongEmailToStrava.Entities;

class StravaActivity
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("sport_type")]
    public required string Type { get; set; }
}
