using Microsoft.AspNetCore.SignalR;

namespace BDKahoot.API.Hubs.Services
{
    public interface ISafeCommunicationService
    {
        Task<bool> SendToGroupSafe(string groupName, string method, object? data = null);
        Task<bool> SendToClientSafe(string connectionId, string method, object? data = null);
        Task<bool> SendToCallerSafe(IClientProxy caller, string method, object? data = null);
        void ExecuteInBackground(Func<Task> operation, string operationName);
        void ExecuteInBackgroundWithDelay(Func<Task> operation, TimeSpan delay, string operationName);
    }
}
