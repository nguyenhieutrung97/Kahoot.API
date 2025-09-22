namespace BDKahoot.API.Hubs.Models.Connections
{
    /// <summary>
    /// Represents connection information for a hub client
    /// </summary>
    public class ConnectionInfo
    {
        public required string ConnectionId { get; set; }
        public required string RoomCode { get; set; }
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public required bool IsHost { get; set; }
    }
}
