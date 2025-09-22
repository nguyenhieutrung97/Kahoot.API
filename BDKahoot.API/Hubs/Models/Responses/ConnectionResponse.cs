using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response data when host disconnects
    /// </summary>
    public class HostDisconnectedResponse
    {
        public string Message { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response data when a player disconnects
    /// </summary>
    public class PlayerDisconnectedResponse
    {
        public string PlayerId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int TotalPlayers { get; set; }
        public int ConnectedPlayers { get; set; }
    }

    /// <summary>
    /// Response data for player reconnection state
    /// </summary>
    public class ReconnectionStateResponse
    {
        public string PlayerId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public bool IsReconnecting { get; set; }
        public string GameState { get; set; } = string.Empty;
        public int CurrentScore { get; set; }
        public int CurrentRank { get; set; }
        public int TotalPlayers { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public bool HasAnsweredCurrentQuestion { get; set; }
        public List<string> CurrentAnswers { get; set; } = new();
        public bool IsWaitingForHost { get; set; }
        public DateTime QuestionStartTime { get; set; }
        public DateTime QuestionEndTime { get; set; }
        public List<PlayerListItem> Players { get; set; } = new();
        public string? GameBackgroundBase64 { get; set; } // Background image for the game in Base64 format(loaded once when session is created)
        public GameAudio? GameAudioUrls { get; set; } // Audio URLs for the game (loaded once when session is created)
    }

    /// <summary>
    /// Response data for other players when someone joins
    /// </summary>
    public class PlayerJoinedNotificationResponse
    {
        public string PlayerId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalPlayers { get; set; }
        public bool IsReconnecting { get; set; }
        public List<PlayerListItem> Players { get; set; } = new();
    }
}
