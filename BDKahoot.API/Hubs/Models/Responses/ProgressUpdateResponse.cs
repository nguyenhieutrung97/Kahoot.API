namespace BDKahoot.API.Hubs.Models.Responses
{
    /// <summary>
    /// Response for sending progress updates to the host during gameplay
    /// </summary>
    public class ProgressUpdateResponse
    {
        public int QuestionIndex { get; set; }
        public int TotalQuestions { get; set; }
        public int PlayersAnswered { get; set; }
        public int TotalPlayers { get; set; }
        public List<PlayerProgressInfo> Players { get; set; } = new();
    }

    /// <summary>
    /// Individual player progress information
    /// </summary>
    public class PlayerProgressInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool HasAnswered { get; set; }
        public int Score { get; set; }
    }
}