using BDKahoot.Domain.Enums;
using System.Buffers.Text;
using System.Collections.Generic;

namespace BDKahoot.Domain.Models
{
    public class GameSession : BaseModel
    {
        public string GameId { get; set; } = default!;
        public string RoomCode { get; set; } = default!; // Unique room code for players to join
        public DateTime StartOn { get; set; }
        public DateTime? EndOn { get; set; }
        public TimeSpan? Duration => EndOn?.Subtract(StartOn); // Calculated duration
        public GameSessionState State { get; set; } = GameSessionState.Lobby;
        public string HostConnectionId { get; set; } = default!; // Host's SignalR connection ID
        public bool AllowReconnection { get; set; } = true; // Allow existing players to reconnect
        public bool AutoShowResults { get; set; } = true; // Auto show results when all players answer, or wait for host to show manually
        
        // Runtime properties (not persisted to database)
        public List<Player> Players { get; set; } = new();
        public int CurrentQuestionIndex { get; set; } = -1;
        public List<Question> Questions { get; set; } = new();
        public DateTime QuestionStartTime { get; set; }
        public DateTime QuestionEndTime { get; set; }
        public bool IsWaitingForHost { get; set; } = false; // True when all players answered, waiting for host to proceed
        public string? GameBackgroundBase64 { get; set; } // Background image for the game in Base64 format(loaded once when session is created)
        public GameAudio? GameAudioUrls { get; set; } // Audio URLs for the game (loaded once when session is created)

        // Statistics
        public int TotalQuestions => Questions?.Count ?? 0;
        public int TotalPlaytime => Questions?.Sum(q => q.TimeLimitSeconds) ?? 0;
        public int PlayersJoined => Players?.Count ?? 0;
        
        // Backward compatibility properties
        public bool IsActive => State == GameSessionState.InProgress || State == GameSessionState.WaitingForHost;
    }
}