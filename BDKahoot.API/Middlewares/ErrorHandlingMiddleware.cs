using BDKahoot.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace BDKahoot.API.Middlewares
{
    public class ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger) : IMiddleware
    {
        private const string InvalidIDMongoDB = "is not a valid 24 digit hex string.";
        private const string UnsupportedMapping = "Missing type map configuration or unsupported mapping";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (NotFoundExceptionCustom notFound)
            {
                LogAndRespond(context, 404, notFound, logLevel: LogLevel.Warning);
            }
            catch (UnauthorizedAccessExceptionCustom unauthorized)
            {
                LogAndRespond(context, 401, unauthorized, logLevel: LogLevel.Warning);
            }
            catch (FileNotFoundException fileNotFound)
            {
                LogAndRespond(context, 404, fileNotFound, logLevel: LogLevel.Warning);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains(InvalidIDMongoDB, StringComparison.OrdinalIgnoreCase))
                {
                    LogAndRespond(context, 400, ex, "Invalid ID format. Must be a 24-character hexadecimal string.");
                }
                else if (ex.Message.Contains(UnsupportedMapping, StringComparison.OrdinalIgnoreCase))
                {
                    LogAndRespond(context, 500, ex, "Mapping configuration error. Please check your AutoMapper setup.");
                }
                else
                {
                    LogAndRespond(context, 500, ex, "Something went wrong!");
                }
            }
        }

        private async void LogAndRespond(HttpContext context, int statusCode, Exception ex, string? messageOverride = null, LogLevel logLevel = LogLevel.Error)
        {
            if (logLevel == LogLevel.Error)
            {
                logger.LogError(ex, ex.Message);
            }
            else
            {
                logger.LogWarning(ex, ex.Message);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(messageOverride ?? ex.Message);
        }
    }
}
