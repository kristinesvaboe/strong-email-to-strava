using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StrongEmailToStrava.Services;

namespace StrongEmailToStrava;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceProvider = new ServiceCollection()
            .AddLogging((loggingBuilder) => loggingBuilder
                .SetMinimumLevel(LogLevel.Trace)
                .AddFilter("Default", LogLevel.Information)
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information)
                .AddConsole()
                )
            .AddSingleton<ITransferWorkoutInfo, TransferWorkoutInfo>()
            .AddSingleton<IStravaService, StravaService>()
            .AddSingleton<IEmailReaderService, EmailReaderService>()
            .AddSingleton<IEmailSenderService, EmailSenderService>()
            .AddScoped<IConfiguration>(_ => configuration)
            .BuildServiceProvider();

        var svc = serviceProvider.GetRequiredService<ITransferWorkoutInfo>();
        await svc.TransferWorkoutToStrava();
    }
}


