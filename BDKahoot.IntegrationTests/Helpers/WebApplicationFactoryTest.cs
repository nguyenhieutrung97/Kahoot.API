using BDKahoot.Domain.Models;
using BDKahoot.Infrastructure.MongoDb;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mongo2Go;
using MongoDB.Driver;

namespace BDKahoot.IntegrationTests.Helpers
{
    public class WebApplicationFactoryTest : WebApplicationFactory<Program>
    {
        private readonly MongoDbRunner _mongoRunner;
        private readonly string _databaseName = "BDKahoot_TestDb";

        public WebApplicationFactoryTest()
        {
            _mongoRunner = MongoDbRunner.Start();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace existing MongoDbSettings
                var dbSettings = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IOptions<MongoDbSettings>)
                );
                if (dbSettings is not null) services.Remove(dbSettings);

                // Inject test MongoDbContext with Mongo2Go
                services.AddSingleton(provider =>
                {
                    var settings = Options.Create(new MongoDbSettings
                    {
                        ConnectionString = _mongoRunner.ConnectionString,
                        DatabaseName = _databaseName
                    });

                    return new MongoDbContext(settings);
                });

                // Setup test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, AzureAdAuthHandlerTest>("Test", _ => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                        .RequireAuthenticatedUser()
                        .Build();
                });

                // Build scope to seed test data
                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
                SeedTestData(db);
            });
        }

        private void SeedTestData(MongoDbContext db)
        {
            var gamesCollection = db.GetCollection<Game>("Games");

            gamesCollection.DeleteMany(_ => true); // Clean existing data

            var testGames = new List<Game>
            {
                new Game
                {
                    Id = "434343434340000000000001",
                    Title = "Sample Game 1",
                    Description = "Description for game 1",
                    HostUserNTID = "host-1",
                    CreatedOn = DateTime.UtcNow
                },
                new Game
                {
                    Id = "434343434340000000000002",
                    Title = "Sample Game 2",
                    Description = "Description for game 2",
                    HostUserNTID = "host-2",
                    CreatedOn = DateTime.UtcNow
                }
            };

            gamesCollection.InsertMany(testGames);
        }

        // Ensure Mongo2Go is cleaned up properly
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mongoRunner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
