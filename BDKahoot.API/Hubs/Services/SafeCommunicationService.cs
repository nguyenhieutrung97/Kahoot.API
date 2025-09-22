using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.API.Hubs.Services
{
    /// <summary>
    /// Provides safe communication methods for SignalR Hub operations
    /// </summary>
    public class SafeCommunicationService : ISafeCommunicationService
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly ILogger<SafeCommunicationService> _logger;

        public SafeCommunicationService(IHubContext<GameHub> hubContext, ILogger<SafeCommunicationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Safely send message to a SignalR group with error handling
        /// </summary>
        public async Task<bool> SendToGroupSafe(string groupName, string method, object? data = null)
        {
            try
            {
                await _hubContext.Clients.Group(groupName).SendAsync(method, data);
                _logger.LogDebug($"Successfully sent {method} to group {groupName}");
                return true;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, $"Hub context disposed while sending {method} to group {groupName}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending {method} to group {groupName}");
                return false;
            }
        }

        /// <summary>
        /// Safely send message to a specific client with error handling
        /// </summary>
        public async Task<bool> SendToClientSafe(string connectionId, string method, object? data = null)
        {
            try
            {
                await _hubContext.Clients.Client(connectionId).SendAsync(method, data);
                _logger.LogDebug($"Successfully sent {method} to client {connectionId}");
                return true;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, $"Hub context disposed while sending {method} to client {connectionId}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending {method} to client {connectionId}");
                return false;
            }
        }

        /// <summary>
        /// Safely send message to caller with error handling
        /// </summary>
        public async Task<bool> SendToCallerSafe(IClientProxy caller, string method, object? data = null)
        {
            try
            {
                await caller.SendAsync(method, data);
                _logger.LogDebug($"Successfully sent {method} to caller");
                return true;
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, $"Hub context disposed while sending {method} to caller");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending {method} to caller");
                return false;
            }
        }

        /// <summary>
        /// Execute an async operation in a background task to avoid hub disposal issues
        /// </summary>
        public void ExecuteInBackground(Func<Task> operation, string operationName)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await operation();
                    _logger.LogDebug($"Background operation {operationName} completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in background operation {operationName}");
                }
            });
        }

        /// <summary>
        /// Execute an async operation with delay in a background task
        /// </summary>
        public void ExecuteInBackgroundWithDelay(Func<Task> operation, TimeSpan delay, string operationName)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay);
                    await operation();
                    _logger.LogDebug($"Delayed background operation {operationName} completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in delayed background operation {operationName}");
                }
            });
        }
    }
}
