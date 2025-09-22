namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Individual player's result for a specific question
    /// </summary>
    public class PlayerQuestionResultResponse
    {
        public int QuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public bool IsMultipleChoice { get; set; }
        public bool IsCorrect { get; set; }
        public int CurrentRank { get; set; }
        public int Score { get; set; }
        public CorrectAnswerInfo? CorrectAnswer { get; set; }
        public List<CorrectAnswerInfo> CorrectAnswers { get; set; } = new();
        public List<QuestionAnswerInfo> Answers { get; set; } = new();
        public object TopPlayers { get; set; } = new();
        public List<string> PlayerAnswers { get; set; } = new();
        public bool IsLastQuestion { get; set; }
        public double CorrectnessRatio { get; set; }
    }

    /// <summary>
    /// Information about correct answers
    /// </summary>
    public class CorrectAnswerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// Information about question answers with statistics
    /// </summary>
    public class QuestionAnswerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int PlayerCount { get; set; }
        public bool Selected { get; set; }
    }
}