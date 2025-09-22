using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using BDKahoot.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BDKahoot.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Load MongoDb settings from appsettings.json
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));

            // Add MongoDbContext as singleton
            services.AddSingleton<MongoDbContext>();

            // Register UnitOfWork & Repositories
            services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
            services.AddScoped<IGameRepository, GameRepository>();
        }
    }
}
