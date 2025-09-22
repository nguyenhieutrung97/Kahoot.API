namespace BDKahoot.API.Hubs.Models.Players
{
    /// <summary>
    /// Represents player information specific to the game lobby view
    /// </summary>
    public class LobbyPlayerInfo
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
