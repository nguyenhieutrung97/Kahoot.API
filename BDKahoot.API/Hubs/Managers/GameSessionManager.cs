using BDKahoot.API.Hubs.Models.Connections;
using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using System.Collections.Concurrent;

namespace BDKahoot.API.Hubs.Managers
{
    /// <summary>
    /// Manages game session state and operations
    /// </summary>
    public class GameSessionManager : IGameSessionManager
    {
        // Store active game sessions in memory (for SignalR specific data)
        private static readonly ConcurrentDictionary<string, GameSession> _gameSessions = new();

        public bool TryGetGameSession(string roomCode, out GameSession? gameSession)
        {
            return _gameSessions.TryGetValue(roomCode, out gameSession);
        }

        public void AddGameSession(string roomCode, GameSession gameSession)
        {
            _gameSessions.TryAdd(roomCode, gameSession);
        }

        public void RemoveGameSession(string roomCode)
        {
            _gameSessions.TryRemove(roomCode, out _);
        }

        public GameSession CreateHubGameSession(string roomCode, string gameId, List<Question> questions, string hostConnectionId, bool autoShowResults = true)
        {
            return new GameSession
            {
                Id = roomCode,
                GameId = gameId,
                State = GameSessionState.Lobby,
                CurrentQuestionIndex = -1,
                Questions = questions,
                Players = new List<Player>(),
                HostConnectionId = hostConnectionId,
                AutoShowResults = autoShowResults,
                AllowReconnection = true
            };
        }

        public void UpdateGameState(string roomCode, GameSessionState state)
        {
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                gameSession.State = state;
            }
        }

        public void ResetPlayersAnswerState(string roomCode)
        {
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                foreach (var player in gameSession.Players)
                {
                    player.HasAnswered = false;
                    player.LastAnswerIds.Clear();
                    player.LastAnswerScores?.Clear();
                }
            }
        }

        public void SetQuestionTimings(string roomCode, DateTime startTime, int timeLimitSeconds)
        {
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                gameSession.QuestionStartTime = startTime;
                gameSession.QuestionEndTime = startTime.AddSeconds(timeLimitSeconds);
                gameSession.IsWaitingForHost = false;
            }
        }

        public List<LeaderboardEntry> GetLeaderboard(string roomCode)
        {
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                return gameSession.Players
                    .OrderByDescending(p => p.Score)
                    .Select((p, index) => new LeaderboardEntry
                    {
                        Rank = index + 1,
                        PlayerId = p.UserId,
                        UserName = p.UserName,
                        Score = p.Score,
                        CorrectAnswers = p.CorrectAnswers,
                        TotalAnswers = p.TotalAnswers,
                        Progress = $"{p.CorrectAnswers}/{p.TotalAnswers}"
                    }).ToList();
            }
            return new List<LeaderboardEntry>();
        }

        public static void ClearAllSessions()
        {
            _gameSessions.Clear();
        }
    }
}
