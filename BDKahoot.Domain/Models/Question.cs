using BDKahoot.Domain.Enums;
using System.Collections.Generic;

namespace BDKahoot.Domain.Models
{
    public class Question : BaseModel
    {
        public string GameId { get; set; } = default!;
        public string Title { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int ScoreValue { get; set; } = 100; // Default score value for correct answers
        public QuestionType Type { get; set; }
    }
}