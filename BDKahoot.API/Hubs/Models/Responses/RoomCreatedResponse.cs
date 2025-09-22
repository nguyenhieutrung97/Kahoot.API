using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response data sent when a game room is successfully created
    /// </summary>
    public class RoomCreatedResponse
    {
        public string RoomCode { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TotalPlaytime { get; set; }
        public bool AutoShowResults { get; set; }
        public string? GameBackgroundBase64 { get; set; } // Background image for the game in Base64 format(loaded once when session is created)
        public GameAudio? GameAudioUrls { get; set; } // Audio URLs for the game (loaded once when session is created)
    }
}
