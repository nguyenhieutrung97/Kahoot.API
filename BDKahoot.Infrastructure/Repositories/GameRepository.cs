using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class GameRepository : GenericRepository<Game>, IGameRepository
    {
        private readonly IMongoCollection<Game> _gameCollection;

        public GameRepository(MongoDbContext context) : base(context, "Games")
        {
            _gameCollection = context.GetCollection<Game>("Games");
        }

        public async Task<Game?> GetByTitleAsync(string title)
        {
            return await _gameCollection.Find(g => g.Title == title && !g.Deleted).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Game>> GetGamesByHostUserNTIDAsync(string hostUserNtid)
        {
            return await _gameCollection.Find(g => g.HostUserNTID == hostUserNtid && !g.Deleted).ToListAsync();
        }

        public async Task<List<Game>> GetByCreatedOnRangeAsync(DateTime startUtc, DateTime endUtc)
        {
            return await _gameCollection.Find(g => g.CreatedOn >= startUtc && g.CreatedOn < endUtc && !g.Deleted).ToListAsync();
        }
    }
}
