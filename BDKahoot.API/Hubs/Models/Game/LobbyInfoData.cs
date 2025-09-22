using BDKahoot.API.Hubs.Models.Players;

namespace BDKahoot.API.Hubs.Models.Game
{
    /// <summary>
    /// Represents game lobby information for display in the UI
    /// </summary>
    public class LobbyInfoData
    {
        public string RoomCode { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TotalPlaytime { get; set; }
        public int PlayerCount { get; set; }
        public List<LobbyPlayerInfo> Players { get; set; } = new();
        public string State { get; set; } = string.Empty;
        public bool CanStart { get; set; }
    }
}
