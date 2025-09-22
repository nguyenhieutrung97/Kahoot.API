namespace BDKahoot.API.Hubs.Models.Players
{
    /// <summary>
    /// Represents player information for display in player lists
    /// </summary>
    public class PlayerListItem
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime JoinedAt { get; set; }
        public int Score { get; set; }
    }
}
