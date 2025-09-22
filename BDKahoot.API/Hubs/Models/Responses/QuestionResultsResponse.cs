using BDKahoot.API.Hubs.Models.Players;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Answer statistics for host view
    /// </summary>
    public class AnswerStatistics
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int PlayerCount { get; set; }
        public bool Selected { get; set; }
    }

    /// <summary>
    /// Response data for question results sent to host
    /// </summary>
    public class QuestionResultsResponse
    {
        public int QuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public bool IsMultipleChoice { get; set; }
        public bool IsLastQuestion { get; set; }
        public int TimeLimitSeconds { get; set; }
        public List<LeaderboardEntry> Leaderboard { get; set; } = new();
        public int PlayersAnswered { get; set; }
        public int TotalPlayers { get; set; }
        public bool HasMoreQuestions { get; set; }
        public List<AnswerStatistics> AnswersWithStats { get; set; } = new();
        public string? Message { get; set; }
        public bool ShowFinalLeaderboardReady { get; set; }
    }
}
