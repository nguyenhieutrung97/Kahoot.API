using BDKahoot.Application.Extensions;
using BDKahoot.Application.Services.AnalyticsService;
using BDKahoot.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";
var configuration = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);

        // Add hosted logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    // Previous UTC calendar date
    DateTime dateToRecord = DateTime.UtcNow.Date.AddDays(-1);

    var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

    logger.LogInformation("Starting daily analytics recording for {Date}", dateToRecord.ToString("yyyy-MM-dd"));
    await analyticsService.RecordGameAnalyticDaily(dateToRecord);
    logger.LogInformation("Finished daily analytics recording for {Date}", dateToRecord.ToString("yyyy-MM-dd"));

    await host.StopAsync();
    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Error while recording daily analytics");
    Environment.ExitCode = 1;
}
