using BDKahoot.Application.Services.Models;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;

namespace BDKahoot.API.Services.ActiveGameService
{
    /// <summary>
    /// Service to manage active game sessions and provide game data to anonymous players
    /// </summary>
    public interface IActiveGameService
    {
        Task<bool> CreateGameSession(string roomCode, string gameId, Game game, List<Question> questions, Dictionary<string, List<Answer>>? questionsAnswers = null);
        Task<GameSessionData?> GetGameSessionData(string roomCode);
        Task<bool> UpdateGameState(string roomCode, GameState newState);
        Task<bool> RemoveGameSession(string roomCode);
        Task<List<string>> GetActiveRoomCodes();
        Task<Game?> GetGameByRoomCode(string roomCode);
        Task<List<Question>?> GetQuestionsByRoomCode(string roomCode);
        Task<Dictionary<string, List<Answer>>?> GetAnswersByRoomCode(string roomCode);
    }
}
