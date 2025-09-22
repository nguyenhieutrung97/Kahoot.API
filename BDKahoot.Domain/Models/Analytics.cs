namespace BDKahoot.Domain.Models
{
    /// <summary>
    /// Analytics model to track game statistics
    /// </summary>
    public class GameAnalytics : BaseModel
    {
        public DateTime Date { get; set; } = DateTime.UtcNow.Date; // Date for daily analytics
        public int GamesCreated { get; set; } = 0;
        public int GameSessionsStarted { get; set; } = 0;
        public int PlayersJoined { get; set; } = 0;
        public int GameSessionsCompleted { get; set; } = 0;
        public int GameSessionsAborted { get; set; } = 0;
        public double AveragePlayersPerSession { get; set; } = 0;
        public double AverageSessionDuration { get; set; } = 0; // In minutes
        
        public void UpdateAverages(int totalSessions, double totalDurationMinutes)
        {
            if (totalSessions > 0)
            {
                AveragePlayersPerSession = (double)PlayersJoined / totalSessions;
                AverageSessionDuration = totalDurationMinutes / totalSessions;
            }
        }
    }
    
    /// <summary>
    /// Detailed session analytics for individual game sessions
    /// </summary>
    public class SessionAnalytics : BaseModel
    {
        public string GameSessionId { get; set; } = default!;
        public string GameId { get; set; } = default!;
        public string RoomCode { get; set; } = default!;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        public int PlayersJoined { get; set; }
        public int QuestionsAnswered { get; set; }
        public int TotalQuestions { get; set; }
        public double CompletionRate => TotalQuestions > 0 ? (double)QuestionsAnswered / TotalQuestions * 100 : 0;
        public string WinnerName { get; set; } = string.Empty;
        public int WinnerScore { get; set; }
        public double AverageScore { get; set; }
        public List<PlayerAnalytics> PlayerStats { get; set; } = new();
    }
    
    /// <summary>
    /// Analytics for individual player performance
    /// </summary>
    public class PlayerAnalytics : BaseModel
    {
        public string SessionAnalyticsId { get; set; } = default!;
        public string PlayerId { get; set; } = default!;
        public string PlayerName { get; set; } = default!;
        public int FinalScore { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalAnswers { get; set; }
        public double AccuracyPercentage { get; set; }
        public double AverageResponseTime { get; set; } // In seconds
        public int FinalRank { get; set; }
        public DateTime JoinTime { get; set; }
        public TimeSpan? PlayDuration { get; set; }
    }
}
