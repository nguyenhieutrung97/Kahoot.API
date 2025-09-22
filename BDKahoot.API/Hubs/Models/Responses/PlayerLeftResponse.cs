using BDKahoot.API.Hubs.Models.Players;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response when a player leaves or is kicked from the game
    /// </summary>
    public class PlayerLeftResponse
    {
        public string PlayerId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool WasKicked { get; set; }
        public int TotalPlayers { get; set; }
        public List<PlayerListItem> Players { get; set; } = new();
    }
}