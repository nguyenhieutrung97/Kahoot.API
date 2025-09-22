using BDKahoot.Domain.Models;

namespace BDKahoot.Domain.Repositories
{
    public interface IGameSessionRepository : IGenericRepository<GameSession>
    {
        Task<GameSession?> GetByRoomCodeAsync(string roomCode);
        Task<IEnumerable<GameSession>> GetByGameIdAsync(string gameId);
        Task<IEnumerable<GameSession>> GetActiveSessionsAsync();
        Task<bool> IsRoomCodeUniqueAsync(string roomCode);
        Task<GameSession?> GetSessionWithPlayersAsync(string roomCode);
    }
}
