using BDKahoot.API.Hubs.Models.Connections;
using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Managers
{
    /// <summary>
    /// Interface for managing game session state and operations
    /// </summary>
    public interface IGameSessionManager
    {
        bool TryGetGameSession(string roomCode, out GameSession? gameSession);
        void AddGameSession(string roomCode, GameSession gameSession);
        void RemoveGameSession(string roomCode);
        GameSession CreateHubGameSession(string roomCode, string gameId, List<Question> questions, string hostConnectionId, bool autoShowResults = true);
        void UpdateGameState(string roomCode, GameSessionState state);
        void ResetPlayersAnswerState(string roomCode);
        void SetQuestionTimings(string roomCode, DateTime startTime, int timeLimitSeconds);
        List<LeaderboardEntry> GetLeaderboard(string roomCode);
    }
}
