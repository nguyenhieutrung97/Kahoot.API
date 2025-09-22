namespace BDKahoot.Domain.Models
{
    public class Player : BaseModel
    {
        public string GameSessionId { get; set; } = default!; // Reference to GameSession
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string ConnectionId { get; set; } = default!; // Current SignalR connection ID
        public int Score { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public int TotalAnswers { get; set; } = 0;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAnsweredAt { get; set; }
        public bool IsConnected { get; set; } = true;
        public double AverageResponseTime { get; set; } = 0; // Average response time in seconds
        
        // Runtime properties (not persisted to database)
        public bool HasAnswered { get; set; } = false; // For current question
        public int CurrentQuestionIndex { get; set; } = -1; // Track which question this player has answered
        public string? LastAnswerId { get; set; } // Last answer submitted for current question (for SingleChoice/TrueFalse)
        public List<string> LastAnswerIds { get; set; } = new(); // Multiple answers for MultipleChoice questions
        public Dictionary<string, int>? LastAnswerScores { get; set; } = new(); // Track points for each selected answer in MultipleChoice
        public TimeSpan? LastAnswerTime { get; set; } // Time taken to answer last question
        public List<TimeSpan> ResponseTimes { get; set; } = new(); // Track all response times for analytics
        
        // Calculated properties
        public string AnswerProgress => $"{CorrectAnswers}/{TotalAnswers}";
        public double AccuracyPercentage => TotalAnswers > 0 ? (double)CorrectAnswers / TotalAnswers * 100 : 0;
        
        // Methods for tracking answers
        public void SubmitAnswer(bool isCorrect, int scoreEarned, TimeSpan timeTaken)
        {
            TotalAnswers++;
            if (isCorrect)
            {
                CorrectAnswers++;
                Score += scoreEarned;
            }
            LastAnsweredAt = DateTime.UtcNow;
            LastAnswerTime = timeTaken;
            ResponseTimes.Add(timeTaken);
            HasAnswered = true;
            
            // Update average response time
            AverageResponseTime = ResponseTimes.Any() ? ResponseTimes.Average(rt => rt.TotalSeconds) : 0;
        }
        
        // NEW: Helper method to add response time and update average
        public void AddResponseTime(DateTime questionStartTime, DateTime answerSubmittedTime)
        {
            var responseTimeSeconds = (answerSubmittedTime - questionStartTime).TotalSeconds;
            var responseTime = TimeSpan.FromSeconds(responseTimeSeconds);
            ResponseTimes.Add(responseTime);
            
            // Update average response time
            AverageResponseTime = ResponseTimes.Any() ? ResponseTimes.Average(rt => rt.TotalSeconds) : 0;
        }
        
        public void ResetForNewQuestion()
        {
            HasAnswered = false;
            LastAnswerId = null;
            LastAnswerIds.Clear();
            LastAnswerScores?.Clear();
            LastAnswerTime = null;
        }
    }
}