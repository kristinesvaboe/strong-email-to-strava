using System.Text.RegularExpressions;
using StrongEmailToStrava.Services;

namespace StrongEmailToStrava;

public interface ITransferWorkoutInfo
{
    Task TransferWorkoutToStrava();
}

class TransferWorkoutInfo : ITransferWorkoutInfo
{
    private readonly IStravaService _stravaService;
    private readonly IEmailReaderService _emailReaderService;
    private readonly IEmailSenderService _emailSenderService;

    public TransferWorkoutInfo(IStravaService stravaService, IEmailReaderService emailReaderService, IEmailSenderService emailSenderService)
    {
        _stravaService = stravaService;
        _emailReaderService = emailReaderService;
        _emailSenderService = emailSenderService;
    }

    public async Task TransferWorkoutToStrava()
    {
        var emails = _emailReaderService.GetEmails();

        if (emails.Count > 0)
        {
            foreach (var uid in emails)
            {
                var message = _emailReaderService.GetEmail(uid);

                var timePattern = @"(?<=at )(2[0-3]|[01]?[0-9]):([0-5]?[0-9])";
                var datePattern = @"(\d{1,2})(\s)(\w+)(\s)(\d{4})";
                var bodyPattern = @"(?<=at (2[0-3]|[01]?[0-9]):([0-5]?[0-9])\s{4})(?s)(.*)(?=\s{2}https)";

                var title = message.Subject;
                var time = Regex.Match(message.TextBody, timePattern, RegexOptions.IgnoreCase).Value;
                var date = Regex.Match(message.TextBody, datePattern, RegexOptions.IgnoreCase).Value;
                var body = Regex.Match(message.TextBody, bodyPattern, RegexOptions.IgnoreCase).Value;

                var datetime = $"{date} {time}";
                DateTime workoutDateTime = DateTime.Parse(datetime);

                var updateStatus = await _stravaService.UpdateStrava(workoutDateTime, title, body);

                if (updateStatus == UpdateStatus.Success)
                {
                    _emailReaderService.MarkEmailAsRead(uid);
                }
                else
                {
                    var subject = "Strong to Strava activity update failed";
                    var messageBody = "";
                    switch (updateStatus)
                    {
                        case UpdateStatus.ActivityNotFound:
                            messageBody = $"No matching Strava activity found for {title} on {datetime}.";
                            break;
                        case UpdateStatus.UpdateFailed:
                            messageBody = $"Update of matched Strava activity for {title} on {datetime} failed.";
                            break;
                        default:
                            messageBody = $"Unknown error while processing {title} on {datetime}.";
                            break;
                    }

                    await _emailSenderService.SendEmailAsync(subject, messageBody);
                }
            }
        }
    }
}


