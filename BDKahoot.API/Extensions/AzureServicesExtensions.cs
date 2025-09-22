using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace BDKahoot.API.Extensions
{
    public static class AzureServicesExtensions
    {
        public static void AddAzureServices(this WebApplicationBuilder builder)
        {
            // Add Azure Application Insights logging
            var applicationInsightConnectionString = builder.Configuration.GetConnectionString("APPLICATIONINSIGHTS_CONNECTION_STRING");
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Logging.AddApplicationInsights(
                    configureTelemetryConfiguration: (config) =>
                        config.ConnectionString = applicationInsightConnectionString,
                        configureApplicationInsightsLoggerOptions: (options) => { }
            );
            builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);

            // Add Azure KeyVault
            var keyVaultConnectionString = builder.Configuration["VaultUri"];
            if (!string.IsNullOrEmpty(keyVaultConnectionString))
            {
                var keyVaultEndpoint = new Uri(keyVaultConnectionString);
                builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
            }

            // Add Azure StorageAccount
            var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage");
            builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
        }
    }
}
