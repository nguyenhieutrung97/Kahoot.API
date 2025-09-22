using BDKahoot.API.Hubs.Models.Players;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response data when game starts
    /// </summary>
    public class GameStartedResponse
    {
        public int TotalQuestions { get; set; }
        public int TotalPlaytime { get; set; }
    }

    /// <summary>
    /// Response data when game completes
    /// </summary>
    public class GameCompletedResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<LeaderboardEntry> FinalLeaderboard { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int TotalPlayers { get; set; }
        public LeaderboardEntry? Winner { get; set; }
        public List<LeaderboardEntry> TopThreePlayers { get; set; } = new();
    }
}
