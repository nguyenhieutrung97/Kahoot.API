namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response when player submits multiple answers for MultipleChoice questions
    /// </summary>
    public class MultipleAnswersSubmittedResponse
    {
        public int QuestionIndex { get; set; }
        public List<string> SelectedAnswers { get; set; } = new();
        public bool IsFinalized { get; set; }
        
        // For SubmitMultipleAnswers method - includes the submitted answer IDs
        public List<string>? AnswerIds { get; set; }
    }
}