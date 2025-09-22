using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class PlayerRepository : GenericRepository<Player>, IPlayerRepository
    {
        public PlayerRepository(MongoDbContext context) : base(context, "Players")
        {
        }

        public async Task<IEnumerable<Player>> GetByGameSessionIdAsync(string gameSessionId)
        {
            return await Collection.Find(x => x.GameSessionId == gameSessionId && !x.Deleted).ToListAsync();
        }

        public async Task<Player?> GetByUserIdAndSessionIdAsync(string userId, string gameSessionId)
        {
            return await Collection.Find(x => x.UserId == userId && x.GameSessionId == gameSessionId && !x.Deleted)
                                   .FirstOrDefaultAsync();
        }

        public async Task<Player?> GetByUserIdAndSessionAsync(string userId, string gameSessionId)
        {
            return await GetByUserIdAndSessionIdAsync(userId, gameSessionId);
        }

        public async Task<Player?> GetByUserNameAndSessionAsync(string userName, string gameSessionId)
        {
            return await Collection.Find(x => x.UserName == userName && x.GameSessionId == gameSessionId && !x.Deleted)
                                   .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Player>> GetTopPlayersBySessionAsync(string gameSessionId, int count = 10)
        {
            return await Collection.Find(x => x.GameSessionId == gameSessionId && !x.Deleted)
                                   .SortByDescending(x => x.Score)
                                   .ThenByDescending(x => x.CorrectAnswers)
                                   .Limit(count)
                                   .ToListAsync();
        }

        public async Task<int> GetPlayerCountBySessionAsync(string gameSessionId)
        {
            return (int)await Collection.CountDocumentsAsync(x => x.GameSessionId == gameSessionId && !x.Deleted);
        }
    }
}
