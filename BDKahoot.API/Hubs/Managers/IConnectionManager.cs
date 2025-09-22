using ConnectionInfo = BDKahoot.API.Hubs.Models.Connections.ConnectionInfo;

namespace BDKahoot.API.Hubs.Managers
{
    /// <summary>
    /// Interface for managing SignalR connection mappings and cleanup operations
    /// </summary>
    public interface IConnectionManager
    {
        void AddConnectionMapping(string connectionId, string roomCode, string userId, bool isHost, string userName);
        ConnectionInfo? GetConnectionInfo(string connectionId);
        bool IsHost(string connectionId);
        void CleanupConnection(string connectionId);
        void CleanupPlayerConnection(string connectionId, string userName);
        (string UserId, string RoomId)? GetUserReconnectionInfo(string userName);
    }
}
