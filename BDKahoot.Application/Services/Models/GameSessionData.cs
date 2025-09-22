using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;

namespace BDKahoot.Application.Services.Models
{
    /// <summary>
    /// Data structure for active game sessions
    /// </summary>
    public class GameSessionData
    {
        public string RoomCode { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public Game Game { get; set; } = null!;
        public List<Question> Questions { get; set; } = new();
        public Dictionary<string, List<Answer>> QuestionsAnswers { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public GameState State { get; set; }
        public bool IsActive { get; set; }
        public int CurrentQuestionIndex { get; set; } = -1;
    }
}
