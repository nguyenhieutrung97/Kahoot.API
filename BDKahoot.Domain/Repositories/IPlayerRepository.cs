using BDKahoot.Domain.Models;

namespace BDKahoot.Domain.Repositories
{
    public interface IPlayerRepository : IGenericRepository<Player>
    {
        Task<IEnumerable<Player>> GetByGameSessionIdAsync(string gameSessionId);
        Task<Player?> GetByUserIdAndSessionAsync(string userId, string gameSessionId);
        Task<Player?> GetByUserIdAndSessionIdAsync(string userId, string gameSessionId);
        Task<Player?> GetByUserNameAndSessionAsync(string userName, string gameSessionId);
        Task<IEnumerable<Player>> GetTopPlayersBySessionAsync(string gameSessionId, int count = 10);
        Task<int> GetPlayerCountBySessionAsync(string gameSessionId);
    }
}
