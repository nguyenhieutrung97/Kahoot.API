using BDKahoot.Domain.Enums;

namespace BDKahoot.Application.Services.Models
{
    public class SessionStatistics
    {
        public string RoomCode { get; set; } = string.Empty;
        public int TotalPlayers { get; set; }
        public int ConnectedPlayers { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalPlaytime { get; set; }
        public TimeSpan? SessionDuration { get; set; }
        public GameSessionState State { get; set; }
    }
}
