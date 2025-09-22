using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace BDKahoot.API.Extensions
{
    public static class JwtAuthenticationExtensions
    {
        public static void AddJwtAuthenticationExtensions(this IServiceCollection services, ConfigurationManager configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(options =>
                {
                    configuration.Bind("AzureAd", options);

                    // Configure token validation parameters
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidateLifetime = true;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);

                    // Configure SignalR JWT authentication
                    options.Events = new SignalRJwtBearerEvents();
                }, options =>
                {
                    configuration.Bind("AzureAd", options);
                });
        }
    }

    public class SignalRJwtBearerEvents : JwtBearerEvents
    {
        public SignalRJwtBearerEvents()
        {
            OnMessageReceived = context =>
            {
                // Support multiple token sources for SignalR
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // If no query token, check Authorization header
                if (string.IsNullOrEmpty(accessToken))
                {
                    var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    {
                        accessToken = authorization.Substring("Bearer ".Length);
                    }
                }

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gameHub"))
                {
                    context.Token = accessToken.ToString().Trim();
                }

                return Task.CompletedTask;
            };

            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<SignalRJwtBearerEvents>>();
                logger?.LogWarning("JWT Authentication failed: {Error}", context.Exception?.Message);

                if (context.Exception?.Message?.Contains("IDX14309") == true)
                {
                    logger?.LogWarning("JWT token format issue - possible encoding problem");
                }

                return Task.CompletedTask;
            };

            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<SignalRJwtBearerEvents>>();
                logger?.LogInformation("JWT Token validated successfully for user: {User}",
                    context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            };

            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<SignalRJwtBearerEvents>>();
                logger?.LogWarning("JWT Authentication challenge: {Error}", context.Error);
                return Task.CompletedTask;
            };
        }
    }
}
