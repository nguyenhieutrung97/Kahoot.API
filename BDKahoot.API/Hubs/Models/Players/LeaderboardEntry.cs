namespace BDKahoot.API.Hubs.Models.Players
{
    /// <summary>
    /// Represents a player entry in the game leaderboard
    /// </summary>
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalAnswers { get; set; }
        public string Progress { get; set; } = string.Empty;
        public double AverageResponseTime { get; set; } = 0; // Average response time in seconds
        public string FormattedAvgResponseTime => $"{AverageResponseTime:F1}s"; // Formatted for display
    }
}
