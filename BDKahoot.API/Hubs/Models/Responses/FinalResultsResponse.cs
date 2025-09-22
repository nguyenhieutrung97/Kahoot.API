using BDKahoot.API.Hubs.Models.Players;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response containing final game results for all participants
    /// </summary>
    public class FinalResultsResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<LeaderboardEntry> FinalLeaderboard { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int TotalPlayers { get; set; }
        public LeaderboardEntry? Winner { get; set; }
        public List<LeaderboardEntry> TopThreePlayers { get; set; } = new();
    }
}