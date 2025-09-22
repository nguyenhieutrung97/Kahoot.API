namespace BDKahoot.Domain.Models
{
    public class Answer : BaseModel
    {
        public string GameId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}