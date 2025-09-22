using BDKahoot.Application.Services.Models;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BDKahoot.API.Services.ActiveGameService;

public class ActiveGameService : IActiveGameService
{
    private static readonly ConcurrentDictionary<string, GameSessionData> _activeGameSessions = new();
    private readonly ILogger<ActiveGameService> _logger;

    public ActiveGameService(ILogger<ActiveGameService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CreateGameSession(string roomCode, string gameId, Game game, List<Question> questions, Dictionary<string, List<Answer>>? questionsAnswers = null)
    {
        try
        {
            var sessionData = new GameSessionData
            {
                RoomCode = roomCode,
                GameId = gameId,
                Game = game,
                Questions = questions,
                QuestionsAnswers = questionsAnswers ?? new Dictionary<string, List<Answer>>(),
                CreatedAt = DateTime.UtcNow,
                State = GameState.InLobby,
                IsActive = true
            };

            var result = _activeGameSessions.TryAdd(roomCode, sessionData);
            
            if (result)
            {
                _logger.LogInformation($"Game session created for room {roomCode}, game {gameId}");
            }
            else
            {
                _logger.LogWarning($"Failed to create game session for room {roomCode} - room already exists");
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating game session for room {roomCode}");
            return false;
        }
    }

    public async Task<GameSessionData?> GetGameSessionData(string roomCode)
    {
        try
        {
            _activeGameSessions.TryGetValue(roomCode, out var sessionData);
            return await Task.FromResult(sessionData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting game session data for room {roomCode}");
            return null;
        }
    }

    public async Task<bool> UpdateGameState(string roomCode, GameState newState)
    {
        try
        {
            if (_activeGameSessions.TryGetValue(roomCode, out var sessionData))
            {
                sessionData.State = newState;
                _logger.LogInformation($"Updated game state for room {roomCode} to {newState}");
                return await Task.FromResult(true);
            }
            
            _logger.LogWarning($"Room {roomCode} not found when trying to update state");
            return await Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating game state for room {roomCode}");
            return false;
        }
    }

    public async Task<bool> RemoveGameSession(string roomCode)
    {
        try
        {
            var result = _activeGameSessions.TryRemove(roomCode, out _);
            
            if (result)
            {
                _logger.LogInformation($"Game session removed for room {roomCode}");
            }
            else
            {
                _logger.LogWarning($"Failed to remove game session for room {roomCode} - room not found");
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing game session for room {roomCode}");
            return false;
        }
    }

    public async Task<List<string>> GetActiveRoomCodes()
    {
        try
        {
            var activeCodes = _activeGameSessions.Keys.ToList();
            return await Task.FromResult(activeCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active room codes");
            return new List<string>();
        }
    }

    public async Task<Game?> GetGameByRoomCode(string roomCode)
    {
        try
        {
            if (_activeGameSessions.TryGetValue(roomCode, out var sessionData))
            {
                return await Task.FromResult(sessionData.Game);
            }
            
            return await Task.FromResult<Game?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting game by room code {roomCode}");
            return null;
        }
    }

    public async Task<List<Question>?> GetQuestionsByRoomCode(string roomCode)
    {
        try
        {
            if (_activeGameSessions.TryGetValue(roomCode, out var sessionData))
            {
                return await Task.FromResult(sessionData.Questions);
            }
            
            return await Task.FromResult<List<Question>?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting questions by room code {roomCode}");
            return null;
        }
    }

    public async Task<Dictionary<string, List<Answer>>?> GetAnswersByRoomCode(string roomCode)
    {
        try
        {
            if (_activeGameSessions.TryGetValue(roomCode, out var sessionData))
            {
                return await Task.FromResult(sessionData.QuestionsAnswers);
            }
            
            return await Task.FromResult<Dictionary<string, List<Answer>>?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting answers by room code {roomCode}");
            return null;
        }
    }
}