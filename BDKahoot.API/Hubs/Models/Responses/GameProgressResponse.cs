namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response data for game progression events
    /// </summary>
    public class ProceedToNextQuestionResponse
    {
        public string Message { get; set; } = string.Empty;
        public int CurrentQuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
    }

    /// <summary>
    /// Response data when question time ends
    /// </summary>
    public class QuestionTimeEndedResponse
    {
        public string Message { get; set; } = string.Empty;
        public int QuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
    }
}
