namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response data sent when a player submits an answer
    /// </summary>
    public class AnswerSubmittedResponse
    {
        public string AnswerId { get; set; } = string.Empty;
        public int QuestionIndex { get; set; }
        public bool IsMultipleChoice { get; set; }
        public List<string> SelectedAnswers { get; set; } = new();
    }
}
