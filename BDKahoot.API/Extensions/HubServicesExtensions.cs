using BDKahoot.API.Hubs.Managers;
using BDKahoot.API.Hubs.Services;

namespace BDKahoot.API.Extensions
{
    public static class HubServicesExtensions
    {
        /// <summary>
        /// Register Hub managers and services for dependency injection
        /// </summary>
        public static IServiceCollection AddHubServices(this IServiceCollection services)
        {
            // Register managers as singletons since they manage static state
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IGameSessionManager, GameSessionManager>();
            
            // Register services as scoped
            services.AddScoped<IPlayerManager, PlayerManager>();
            services.AddScoped<ISafeCommunicationService, SafeCommunicationService>();
            services.AddScoped<IGameValidationService, GameValidationService>();
            
            return services;
        }
    }
}
