using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class AnalyticsRepository : GenericRepository<GameAnalytics>, IAnalyticsRepositories
    {
        public AnalyticsRepository(MongoDbContext context) : base(context, "GameAnalytics")
        {
        }

        public async Task<GameAnalytics?> GetByDateAsync(DateTime date)
        {
            var dateOnly = date.Date;
            return await Collection.Find(x => x.Date == dateOnly && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<GameAnalytics>> GetDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var startDateOnly = startDate.Date;
            var endDateOnly = endDate.Date;
            return await Collection.Find(x => x.Date >= startDateOnly && x.Date <= endDateOnly && !x.Deleted)
                                   .SortBy(x => x.Date)
                                   .ToListAsync();
        }

        public async Task UpdateDailyAnalyticsAsync(DateTime date, Action<GameAnalytics> updateAction)
        {
            var dateOnly = date.Date;
            var analytics = await GetByDateAsync(dateOnly);
            
            if (analytics == null)
            {
                analytics = new GameAnalytics 
                { 
                    Date = dateOnly,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                updateAction(analytics);
                await AddAsync(analytics);
            }
            else
            {
                updateAction(analytics);
                analytics.UpdatedOn = DateTime.UtcNow;
                await UpdateAsync(analytics);
            }
        }
    }

    public class SessionAnalyticsRepository : GenericRepository<SessionAnalytics>, ISessionAnalyticsRepository
    {
        public SessionAnalyticsRepository(MongoDbContext context) : base(context, "SessionAnalytics")
        {
        }

        public async Task<SessionAnalytics?> GetByGameSessionIdAsync(string gameSessionId)
        {
            return await Collection.Find(x => x.GameSessionId == gameSessionId && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SessionAnalytics>> GetByGameIdAsync(string gameId)
        {
            return await Collection.Find(x => x.GameId == gameId && !x.Deleted)
                                   .SortByDescending(x => x.StartTime)
                                   .ToListAsync();
        }

        public async Task<IEnumerable<SessionAnalytics>> GetCompletedSessionsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var filter = Builders<SessionAnalytics>.Filter.And(
                Builders<SessionAnalytics>.Filter.Ne(x => x.EndTime, null),
                Builders<SessionAnalytics>.Filter.Eq(x => x.Deleted, false)
            );
            
            if (startDate.HasValue)
                filter &= Builders<SessionAnalytics>.Filter.Gte(x => x.StartTime, startDate.Value);
            
            if (endDate.HasValue)
                filter &= Builders<SessionAnalytics>.Filter.Lte(x => x.StartTime, endDate.Value);

            return await Collection.Find(filter)
                                   .SortByDescending(x => x.StartTime)
                                   .ToListAsync();
        }
    }

    public class PlayerAnalyticsRepository : GenericRepository<PlayerAnalytics>, IPlayerAnalyticsRepository
    {
        public PlayerAnalyticsRepository(MongoDbContext context) : base(context, "PlayerAnalytics")
        {
        }

        public async Task<IEnumerable<PlayerAnalytics>> GetBySessionAnalyticsIdAsync(string sessionAnalyticsId)
        {
            return await Collection.Find(x => x.SessionAnalyticsId == sessionAnalyticsId && !x.Deleted)
                                   .SortBy(x => x.FinalRank)
                                   .ToListAsync();
        }

        public async Task<IEnumerable<PlayerAnalytics>> GetPlayerHistoryAsync(string playerName)
        {
            return await Collection.Find(x => x.PlayerName == playerName && !x.Deleted)
                                   .SortByDescending(x => x.CreatedOn)
                                   .ToListAsync();
        }
    }
}
