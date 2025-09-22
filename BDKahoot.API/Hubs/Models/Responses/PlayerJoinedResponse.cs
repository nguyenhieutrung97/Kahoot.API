using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response data sent when a player successfully joins a game
    /// </summary>
    public class PlayerJoinedResponse
    {
        public string PlayerId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public bool IsReconnecting { get; set; }
        public string GameState { get; set; } = string.Empty;
        public int TotalPlayers { get; set; }
        public List<PlayerListItem> Players { get; set; } = new();
        public int CurrentScore { get; set; }
        public int CurrentRank { get; set; }
        public string? GameBackgroundBase64 { get; set; } // Background image for the game in Base64 format(loaded once when session is created)
        public GameAudio? GameAudioUrls { get; set; } // Audio URLs for the game (loaded once when session is created)
    }
}
