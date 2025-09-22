namespace BDKahoot.API.Constants
{
    /// <summary>
    /// Constants for SignalR event names to ensure consistency and maintainability
    /// </summary>
    public static class SignalREvents
    {
        // Host Events (Outbound)
        public const string RoomCreated = "RoomCreated";
        public const string LobbyInfo = "LobbyInfo";
        public const string HostNewQuestion = "HostNewQuestion";
        public const string QuestionResults = "QuestionResults";
        public const string GameCompleted = "GameCompleted";

        // Player Events (Outbound)
        public const string JoinedGame = "JoinedGame";
        public const string NewQuestion = "NewQuestion";
        public const string AnswerSubmitted = "AnswerSubmitted";
        public const string MultipleAnswersSubmitted = "MultipleAnswersSubmitted"; 
        public const string PlayerQuestionResult = "PlayerQuestionResult";
        public const string FinalResults = "FinalResults";
        public const string GameEnded = "GameEnded"; 
        public const string ReconnectState = "ReconnectState";
        public const string KickedFromGame = "KickedFromGame";

        // Shared Events (Both Host & Players)
        public const string PlayerJoined = "PlayerJoined";
        public const string PlayerLeft = "PlayerLeft";
        public const string PlayerDisconnected = "PlayerDisconnected";
        public const string GameStarted = "GameStarted";
        public const string QuestionTimeEnded = "QuestionTimeEnded";
        public const string ProceedingToNextQuestion = "ProceedingToNextQuestion";
        public const string HostDisconnected = "HostDisconnected";
        public const string Error = "Error";

        // Hub Methods (Inbound)
        public const string CreateGameRoom = "CreateGameRoom";
        public const string KickPlayer = "KickPlayer";
        public const string JoinGame = "JoinGame";
        public const string StartGame = "StartGame";
        public const string SubmitAnswer = "SubmitAnswer";
        public const string SubmitMultipleAnswers = "SubmitMultipleAnswers";
        public const string ProceedToNextQuestion = "ProceedToNextQuestion";
        public const string ShowFinalLeaderboard = "ShowFinalLeaderboard";
    }
}
