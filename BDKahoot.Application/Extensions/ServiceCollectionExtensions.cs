using BDKahoot.Application.Services.AnalyticsService;
using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Application.Services.GameSessionService;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace BDKahoot.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplication(this IServiceCollection services)
        {
            var applicationAssembly = typeof(ServiceCollectionExtensions).Assembly;

            // Register AutoMapper.
            services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

            // Register MediatR.
            services.AddMediatR(config => config.RegisterServicesFromAssembly(applicationAssembly));

            // Register FluentValidation.
            services.AddValidatorsFromAssembly(applicationAssembly).AddFluentValidationAutoValidation();

            // Register Services.
            //services.AddScoped<IBlobStorageService, BlobStorageService>();
            services.AddScoped<IGameSessionService, GameSessionService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
        }
    }
}
