using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StrongEmailToStrava.Configuration;
using StrongEmailToStrava.Entities;

namespace StrongEmailToStrava.Services;

interface IStravaService
{
    Task<UpdateStatus> UpdateStrava(DateTime workoutDateTime, string title, string body);
}

class StravaService : IStravaService
{
    private readonly HttpClient _httpClient = new HttpClient();
    private StravaTokenData _tokens = new StravaTokenData();
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private StravaSettings _settings;

    public StravaService(IConfiguration config, ILogger<StravaService> logger)
    {
        _config = config;
        _logger = logger;
        _settings = _config.GetSection("StravaSettings").Get<StravaSettings>()!;
    }

    public async Task<UpdateStatus> UpdateStrava(DateTime workoutDateTime, string title, string body)
    {
        var activityId = await GetActivity(workoutDateTime);

        if (string.IsNullOrEmpty(activityId))
        {
            return UpdateStatus.ActivityNotFound;
        }

        var success = await UpdateActivity(activityId, title, body);

        if (!success)
        {
            return UpdateStatus.UpdateFailed;
        }

        return UpdateStatus.Success;
    }


    async Task<String> GetAccessToken()
    {
        if (!string.IsNullOrEmpty(_tokens.AccessToken) && _tokens.TokenExpiry > ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds())
        {
            return _tokens.AccessToken;
        }
        else
        {
            await RefreshAccessToken();
            return _tokens.AccessToken;
        }
    }

    async Task RefreshAccessToken()
    {
        try
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://www.strava.com/oauth/token"))
            {
                request.Content = JsonContent.Create(new
                {
                    client_id = _settings.ClientId,
                    client_secret = _settings.ClientSecret,
                    refresh_token = _settings.RefreshToken,
                    grant_type = _settings.GrantType
                });

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();

                var result = !string.IsNullOrEmpty(responseString) ? JsonConvert.DeserializeObject<StravaTokenData>(responseString) : null;
                if (result == null)
                {
                    throw new Exception("Failed to retrieve token");
                }
                _tokens = result;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Error trying to refresh access token: {0}", e);
        }
    }

    async Task<string?> GetActivity(DateTime workoutDateTime)
    {
        long unixTime = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(workoutDateTime), TimeSpan.Zero).ToUnixTimeSeconds();

        // Look for Strava activities +/- 1 hour from the recorded Strong activity
        long afterTime = unixTime - 3600;
        long beforeTime = unixTime + 3600;

        try
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, "https://www.strava.com/api/v3/athlete/activities"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken());
                request.Content = JsonContent.Create(new
                {
                    after = afterTime,
                    before = beforeTime

                });

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();

                var activities = JsonConvert.DeserializeObject<List<StravaActivity>>(responseString);
                var activityId = activities?.Where(a => a.Type == "WeightTraining").FirstOrDefault()?.Id;

                if (activityId == null)
                {
                    _logger.LogError("No matching Strava activity found between {0} and {1}.", beforeTime, afterTime);
                }

                return activityId;
            }
        }
        catch (HttpRequestException)
        {
            _logger.LogError("HTTP error while trying to get activity between {0} and {1}", beforeTime, afterTime);
            return null;
        }
    }

    async Task<bool> UpdateActivity(string activityId, string title, string body)
    {
        try
        {
            using (var request = new HttpRequestMessage(HttpMethod.Put, $"https://www.strava.com/api/v3/activities/{activityId}"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken());
                request.Content = JsonContent.Create(new
                {
                    name = title,
                    description = body
                });

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Strava activity {0} was updated.", activityId);
                return true;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Error while trying to update Strava activity {0} activity: {1}", activityId, e);
            return false;
        }
    }
}