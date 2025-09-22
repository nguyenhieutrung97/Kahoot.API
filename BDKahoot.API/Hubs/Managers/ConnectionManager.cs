using System.Collections.Concurrent;
using ConnectionInfo = BDKahoot.API.Hubs.Models.Connections.ConnectionInfo;

namespace BDKahoot.API.Hubs.Managers
{
    /// <summary>
    /// Manages SignalR connection mappings and cleanup operations
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        // Store connection mappings
        private static readonly ConcurrentDictionary<string, string> _connectionToRoomMap = new();
        private static readonly ConcurrentDictionary<string, string> _connectionToUserIdMap = new();
        private static readonly ConcurrentDictionary<string, bool> _connectionIsHostMap = new();
        private static readonly ConcurrentDictionary<string, string> _connectionToUserNameMap = new();

        // Store player's connection info for reconnection
        private static readonly ConcurrentDictionary<string, (string UserId, string RoomId)> _userIdInfo = new();

        public void AddConnectionMapping(string connectionId, string roomCode, string userId, bool isHost, string userName)
        {
            _connectionToRoomMap.TryAdd(connectionId, roomCode);
            _connectionToUserIdMap.TryAdd(connectionId, userId);
            _connectionIsHostMap.TryAdd(connectionId, isHost);
            _connectionToUserNameMap.TryAdd(connectionId, userName);

            if (!isHost)
            {
                _userIdInfo.TryAdd(userName, (userId, roomCode));
            }
        }

        public ConnectionInfo? GetConnectionInfo(string connectionId)
        {
            _connectionToRoomMap.TryGetValue(connectionId, out var roomCode);
            _connectionToUserIdMap.TryGetValue(connectionId, out var userId);
            _connectionToUserNameMap.TryGetValue(connectionId, out var userName);
            _connectionIsHostMap.TryGetValue(connectionId, out var isHost);

            if (string.IsNullOrEmpty(roomCode))
                return null;

            return new ConnectionInfo
            {
                ConnectionId = connectionId,
                RoomCode = roomCode,
                UserId = userId ?? "",
                UserName = userName ?? "",
                IsHost = isHost
            };
        }

        public bool IsHost(string connectionId)
        {
            return _connectionIsHostMap.TryGetValue(connectionId, out var isHost) && isHost;
        }

        public void CleanupConnection(string connectionId)
        {
            _connectionToRoomMap.TryRemove(connectionId, out var roomCode);
            _connectionToUserIdMap.TryRemove(connectionId, out var userId);
            _connectionIsHostMap.TryRemove(connectionId, out var isHost);
            _connectionToUserNameMap.TryRemove(connectionId, out var userName);

            // Clean up user info mapping for players
            if (!string.IsNullOrEmpty(userName) && !isHost)
            {
                _userIdInfo.TryRemove(userName, out _);
            }
        }

        public void CleanupPlayerConnection(string connectionId, string userName)
        {
            _connectionToRoomMap.TryRemove(connectionId, out _);
            _connectionToUserIdMap.TryRemove(connectionId, out _);
            _connectionIsHostMap.TryRemove(connectionId, out _);
            _connectionToUserNameMap.TryRemove(connectionId, out _);

            if (!string.IsNullOrEmpty(userName))
            {
                _userIdInfo.TryRemove(userName, out _);
            }
        }

        public (string UserId, string RoomId)? GetUserReconnectionInfo(string userName)
        {
            if (_userIdInfo.TryGetValue(userName, out var info))
            {
                return info;
            }
            return null;
        }

        public static void ClearAllConnections()
        {
            _connectionToRoomMap.Clear();
            _connectionToUserIdMap.Clear();
            _connectionIsHostMap.Clear();
            _connectionToUserNameMap.Clear();
            _userIdInfo.Clear();
        }
    }
}
