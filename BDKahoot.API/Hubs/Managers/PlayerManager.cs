using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.API.Hubs.Models.Game;
using BDKahoot.Application.Services.GameSessionService;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Managers
{
    /// <summary>
    /// Manages player-specific operations within game sessions
    /// </summary>
    public class PlayerManager : IPlayerManager
    {
        private readonly IGameSessionService _gameSessionService;
        private readonly ILogger<PlayerManager> _logger;

        public PlayerManager(IGameSessionService gameSessionService, ILogger<PlayerManager> logger)
        {
            _gameSessionService = gameSessionService;
            _logger = logger;
        }

        public Task<PlayerValidationResult> ValidatePlayerJoin(GameSession gameSession, string userName, string connectionId, string? playerId)
        {
            var result = new PlayerValidationResult();

            var existingPlayerWithSameName = gameSession.Players.FirstOrDefault(p => p.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (existingPlayerWithSameName != null && playerId == null)
            {
                result.ExistingPlayer = existingPlayerWithSameName;
                result.IsReconnection = false;
                return Task.FromResult(result);
            }

            if (existingPlayerWithSameName != null && existingPlayerWithSameName.UserId == playerId)
            {
                result.ExistingPlayer = existingPlayerWithSameName;
                result.IsReconnection = true;
            }

            return Task.FromResult(result);
        }

        public async Task<Player?> CreateNewPlayer(string roomCode, string userName, string connectionId)
        {
            try
            {
                var newPlayer = await _gameSessionService.AddPlayerToSessionAsync(roomCode, userName, connectionId);

                return new Player
                {
                    UserId = newPlayer.Id!,
                    UserName = userName,
                    GameSessionId = roomCode,
                    ConnectionId = connectionId,
                    Score = 0,
                    HasAnswered = false,
                    CurrentQuestionIndex = -1,
                    IsConnected = true,
                    JoinedAt = DateTime.UtcNow,
                    AverageResponseTime = 0,  // Initialize response time tracking
                    ResponseTimes = new List<TimeSpan>() // Initialize response times list
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating new player {userName} for room {roomCode}");
                return null;
            }
        }

        public Player CreateHubPlayerFromDb(Domain.Models.Player dbPlayer, string connectionId)
        {
            return new Player
            {
                UserId = dbPlayer.Id!,  // Hub Player.UserId = Database Player.Id
                UserName = dbPlayer.UserName,
                GameSessionId = dbPlayer.GameSessionId,
                ConnectionId = connectionId,
                Score = dbPlayer.Score,
                HasAnswered = dbPlayer.HasAnswered,
                CurrentQuestionIndex = dbPlayer.CurrentQuestionIndex,
                CorrectAnswers = dbPlayer.CorrectAnswers,
                TotalAnswers = dbPlayer.TotalAnswers,
                IsConnected = true,
                JoinedAt = dbPlayer.JoinedAt,
                AverageResponseTime = dbPlayer.AverageResponseTime, // Restore response time data
                ResponseTimes = new List<TimeSpan>() // Reset for new session (runtime property)
            };
        }

        public async Task<bool> ReconnectPlayer(string roomCode, string playerId, string connectionId)
        {
            try
            {
                await _gameSessionService.ReconnectPlayerAsync(roomCode, playerId, connectionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reconnecting player {playerId} to room {roomCode}");
                return false;
            }
        }

        /// <summary>
        /// Update player connection information for reconnection
        /// </summary>
        public void UpdatePlayerConnectionInfo(Player player, string newConnectionId)
        {
            var oldConnectionId = player.ConnectionId;
            player.ConnectionId = newConnectionId;
            player.IsConnected = true;

            _logger.LogInformation($"Updated player {player.UserName} connection from {oldConnectionId} to {newConnectionId}");
        }

        /// <summary>
        /// Mark player as disconnected
        /// </summary>
        public void MarkPlayerAsDisconnected(Player player)
        {
            player.IsConnected = false;
            _logger.LogInformation($"Marked player {player.UserName} as disconnected");
        }

        public async Task<bool> RemovePlayerFromSession(string roomCode, string playerId)
        {
            try
            {
                await _gameSessionService.RemovePlayerFromSessionAsync(roomCode, playerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing player {playerId} from session {roomCode}");
                return false;
            }
        }

        public List<PlayerListItem> CreatePlayerListData(List<Player> players)
        {
            return players.Select(p => new PlayerListItem
            {
                PlayerId = p.UserId,
                Name = p.UserName,
                UserName = p.UserName,
                IsConnected = p.IsConnected,
                JoinedAt = p.JoinedAt,
                Score = p.Score
            }).ToList();
        }

        public LobbyInfoData CreateLobbyInfoData(string roomCode, GameSession gameSession)
        {
            return new LobbyInfoData
            {
                RoomCode = roomCode,
                TotalQuestions = gameSession.Questions.Count,
                TotalPlaytime = gameSession.Questions.Sum(q => q.TimeLimitSeconds),
                PlayerCount = gameSession.Players.Count,
                Players = gameSession.Players.Select(p => new LobbyPlayerInfo
                {
                    PlayerId = p.UserId,
                    Name = p.UserName,
                    ConnectionId = p.ConnectionId,
                    IsConnected = p.IsConnected,
                    JoinedAt = p.JoinedAt
                }).ToList(),
                State = gameSession.State.ToString(),
                CanStart = gameSession.Players.Count > 0
            };
        }

        /// <summary>
        /// Create leaderboard from players
        /// </summary>
        public List<LeaderboardEntry> CreateLeaderboard(IEnumerable<Player> players)
        {
            return players
                .OrderByDescending(p => p.Score)
                .Select((p, index) => new LeaderboardEntry
                {
                    Rank = index + 1,
                    PlayerId = p.UserId,
                    UserName = p.UserName,
                    Score = p.Score,
                    CorrectAnswers = p.CorrectAnswers,
                    TotalAnswers = p.TotalAnswers,
                    Progress = $"{p.CorrectAnswers}/{p.TotalAnswers}",
                    AverageResponseTime = p.AverageResponseTime // Include response time data
                })
                .ToList();
        }

        /// <summary>
        /// Get player rank in the game
        /// </summary>
        public int GetPlayerRank(IEnumerable<Player> players, string playerId)
        {
            var leaderboard = players
                .OrderByDescending(p => p.Score)
                .ToList();

            var rank = leaderboard.FindIndex(p => p.UserId == playerId) + 1;
            return rank > 0 ? rank : players.Count();
        }

        /// <summary>
        /// Calculate if player answered correctly based on question type
        /// </summary>
        /// <remarks>
        /// - For MultipleChoice questions:
        ///   1. Must select ALL correct answers and NO incorrect answers to be considered correct
        ///   2. Binary result: either 100% correct or 0% correct
        ///   3. Points are then calculated based on response time if correct
        /// - For SingleChoice/TrueFalse questions:
        ///   1. Binary scoring: correct (1.0) or incorrect (0.0)
        ///   2. Points are then calculated based on response time if correct
        /// </remarks>
        public (bool answeredCorrect, double correctnessRatio) CalculatePlayerCorrectness(
            Player player, Question question, IEnumerable<Answer> correctAnswers, int currentQuestionIndex)
        {
            bool answeredCorrect = false;
            double correctnessRatio = 0.0;

            if (player.HasAnswered && player.CurrentQuestionIndex == currentQuestionIndex)
            {
                if (question.Type == QuestionType.MultipleChoice)
                {
                    var correctAnswerIds = correctAnswers.Select(ca => ca.Id).ToHashSet();
                    var playerAnswerIds = player.LastAnswerIds.ToHashSet();

                    var hasAllCorrectAnswers = correctAnswerIds.SetEquals(playerAnswerIds);
                    
                    if (hasAllCorrectAnswers)
                    {
                        answeredCorrect = true;
                        correctnessRatio = 1.0; 
                        _logger.LogInformation($"Player {player.UserName} MultipleChoice: PERFECT - selected all {correctAnswerIds.Count} correct answers and no incorrect ones");
                    }
                    else
                    {
                        answeredCorrect = false;
                        correctnessRatio = 0.0;
                        
                        var correctSelections = playerAnswerIds.Count(id => correctAnswerIds.Contains(id));
                        var incorrectSelections = playerAnswerIds.Count(id => !correctAnswerIds.Contains(id));
                        var missedCorrect = correctAnswerIds.Count(id => !playerAnswerIds.Contains(id));
                        
                        _logger.LogInformation($"Player {player.UserName} MultipleChoice: INCORRECT - correct: {correctSelections}/{correctAnswerIds.Count}, incorrect: {incorrectSelections}, missed: {missedCorrect}");
                    }
                }
                else
                {
                    // For single choice and true/false, check if selected answer is correct
                    answeredCorrect = player.LastAnswerIds.Any() && correctAnswers.Any(ca => ca.Id == player.LastAnswerIds.First());
                    correctnessRatio = answeredCorrect ? 1.0 : 0.0;
                }
            }

            return (answeredCorrect, correctnessRatio);
        }
    }
}
