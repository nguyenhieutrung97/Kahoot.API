using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Domain.Enums;
using Microsoft.Extensions.Logging;
using BDKahoot.Application.Services.Models;

namespace BDKahoot.Application.Services.GameSessionService
{
    public class GameSessionService : IGameSessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GameSessionService> _logger;

        public GameSessionService(IUnitOfWork unitOfWork, ILogger<GameSessionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GameSession> CreateGameSessionAsync(string gameId, string hostConnectionId)
        {
            // Generate unique room code with validation
            string roomCode;
            int attempts = 0;
            const int maxAttempts = 10;
            
            do
            {
                roomCode = GenerateRoomCode();
                attempts++;
                
                // Check if room code already exists
                var existingSession = await _unitOfWork.GameSessions.GetByRoomCodeAsync(roomCode);
                if (existingSession == null)
                    break;
                    
                if (attempts >= maxAttempts)
                    throw new InvalidOperationException("Failed to generate unique room code after multiple attempts");
                    
            } while (true);

            var gameSession = new GameSession
            {
                GameId = gameId,
                RoomCode = roomCode,
                StartOn = DateTime.UtcNow,
                State = GameSessionState.Lobby,
                HostConnectionId = hostConnectionId,
                AllowReconnection = true
            };

            // Update game state to InLobby when session is created
            var game = await _unitOfWork.Games.GetByIdAsync(gameId);
            if (game != null)
            {
                game.State = GameState.InLobby;
                await _unitOfWork.Games.UpdateAsync(game);
            }

            await _unitOfWork.GameSessions.AddAsync(gameSession);

            _logger.LogInformation($"Created game session {gameSession.Id} with room code {roomCode} for game {gameId}");
            return gameSession;
        }

        public async Task<GameSession?> GetActiveSessionAsync(string roomCode)
        {
            return await _unitOfWork.GameSessions.GetByRoomCodeAsync(roomCode);
        }

        public async Task<Player> AddPlayerToSessionAsync(string roomCode, string userName, string connectionId)
        {
            var session = await GetActiveSessionAsync(roomCode);
            if (session == null)
                throw new ArgumentException("Game session not found", nameof(roomCode));

            if (session.State != GameSessionState.Lobby)
                throw new InvalidOperationException("Cannot add players to a session that is not in lobby state");

            var player = new Player
            {
                GameSessionId = session.Id!,
                UserName = userName,
                ConnectionId = connectionId,
                JoinedAt = DateTime.UtcNow,
                IsConnected = true
            };

            await _unitOfWork.Players.AddAsync(player);

            _logger.LogInformation($"Added player {userName} to session {roomCode}");
            return player;
        }

        public async Task RemovePlayerFromSessionAsync(string roomCode, string userId)
        {
            var session = await GetActiveSessionAsync(roomCode);
            if (session == null)
                throw new ArgumentException("Game session not found", nameof(roomCode));

            var player = await _unitOfWork.Players.GetByUserIdAndSessionAsync(userId, session.Id!);
            if (player != null)
            {
                await _unitOfWork.Players.DeleteAsync(player.Id!);
                _logger.LogInformation($"Removed player {player.UserName} from session {roomCode}");
            }
        }

        public async Task<Player> ReconnectPlayerAsync(string roomCode, string playerId, string newConnectionId)
        {
            try
            {
                var session = await GetActiveSessionAsync(roomCode);
                if (session == null)
                {
                    _logger.LogWarning($"Game session not found for room code {roomCode} during reconnection");
                    throw new ArgumentException("Game session not found", nameof(roomCode));
                }

                // Try to find player by playerId first
                var player = await _unitOfWork.Players.GetByIdAsync(playerId);
                if (player == null || player.GameSessionId != session.Id)
                {
                    _logger.LogWarning($"Player {playerId} not found in session {session.Id} during reconnection");
                    throw new ArgumentException("Player not found in this session", nameof(playerId));
                }

                // Update connection info
                player.ConnectionId = newConnectionId;
                player.IsConnected = true;
                
                await _unitOfWork.Players.UpdateAsync(player);

                _logger.LogInformation($"Player {player.UserName} (ID: {playerId}) successfully reconnected to session {roomCode}");
                return player;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to reconnect player {playerId} to session {roomCode}");
                throw;
            }
        }

        public async Task StartGameSessionAsync(string roomCode)
        {
            var session = await GetActiveSessionAsync(roomCode);
            if (session == null)
                throw new ArgumentException("Game session not found", nameof(roomCode));

            if (session.State != GameSessionState.Lobby)
                throw new InvalidOperationException("Game session is not in lobby state");

            session.State = GameSessionState.InProgress;
            session.StartOn = DateTime.UtcNow;
            await _unitOfWork.GameSessions.UpdateAsync(session);

            _logger.LogInformation($"Started game session {session.Id} with room code {roomCode}");
        }

        public async Task EndGameSessionAsync(string roomCode, bool hostDisconnected = false)
        {
            var session = await GetActiveSessionAsync(roomCode);
            if (session == null)
                throw new ArgumentException("Game session not found", nameof(roomCode));

            session.State = hostDisconnected ? GameSessionState.Aborted : GameSessionState.Completed;
            session.EndOn = DateTime.UtcNow;
            await _unitOfWork.GameSessions.UpdateAsync(session);

            // Move game state back to Active when session is done (requirement 2)
            var game = await _unitOfWork.Games.GetByIdAsync(session.GameId);
            if (game != null)
            {
                game.State = GameState.Active;
                await _unitOfWork.Games.UpdateAsync(game);
            }
            
            _logger.LogInformation($"Ended game session {session.Id} with room code {roomCode}");
        }

        public async Task<IEnumerable<Player>> GetSessionLeaderboardAsync(string roomCode)
        {
            var session = await GetActiveSessionAsync(roomCode);
            if (session == null)
                return Enumerable.Empty<Player>();

            return await _unitOfWork.Players.GetTopPlayersBySessionAsync(session.Id!, 100);
        }

        public async Task<SessionStatistics> GetSessionStatisticsAsync(string roomCode)
        {
            var session = await GetActiveSessionAsync(roomCode);
            if (session == null)
                throw new ArgumentException("Game session not found", nameof(roomCode));

            var players = await _unitOfWork.Players.GetByGameSessionIdAsync(session.Id!);
            var playersList = players.ToList();

            return new SessionStatistics
            {
                RoomCode = roomCode,
                TotalPlayers = playersList.Count,
                ConnectedPlayers = playersList.Count(p => p.IsConnected),
                AverageScore = playersList.Any() ? playersList.Average(p => p.Score) : 0,
                HighestScore = playersList.Any() ? playersList.Max(p => p.Score) : 0,
                TotalQuestions = session.TotalQuestions,
                TotalPlaytime = session.TotalPlaytime,
                SessionDuration = session.Duration,
                State = session.State
            };
        }

        /// <summary>
        /// Generate a unique 6-character room code
        /// Uses characters that are easy to read and communicate verbally
        /// </summary>
        private string GenerateRoomCode()
        {
            // Exclude ambiguous characters like 0, O, 1, I, etc.
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
