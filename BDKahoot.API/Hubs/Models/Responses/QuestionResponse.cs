using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Answer option data for questions
    /// </summary>
    public class AnswerOption
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// Answer option data for host with correct answer information
    /// </summary>
    public class HostAnswerOption : AnswerOption
    {
        public bool IsCorrect { get; set; }
    }

    /// <summary>
    /// Response data for new question sent to players
    /// </summary>
    public class QuestionResponse
    {
        public int QuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public bool IsMultipleChoice { get; set; }
        public bool IsLastQuestion { get; set; }
        public int TimeLimitSeconds { get; set; }
        public List<AnswerOption> Answers { get; set; } = new();
        public DateTime StartTime { get; set; }
        public string? BackgroundImageBase64 { get; set; }
        public GameAudio? GameAudioUrls { get; set; } // Audio URLs for the game (loaded once when session is created)
    }

    /// <summary>
    /// Response data for new question sent to host (includes correct answers)
    /// </summary>
    public class HostQuestionResponse : QuestionResponse
    {
        public new List<HostAnswerOption> Answers { get; set; } = new();
        public HostAnswerOption? CorrectAnswer { get; set; }
        public List<HostAnswerOption> CorrectAnswers { get; set; } = new();
    }
}
