using BDKahoot.Domain.Models;

namespace BDKahoot.Domain.Repositories
{
    public interface IAnalyticsRepositories : IGenericRepository<GameAnalytics>
    {
        Task<GameAnalytics?> GetByDateAsync(DateTime date);
        Task<IEnumerable<GameAnalytics>> GetDateRangeAsync(DateTime startDate, DateTime endDate);
        Task UpdateDailyAnalyticsAsync(DateTime date, Action<GameAnalytics> updateAction);
    }
    
    public interface ISessionAnalyticsRepository : IGenericRepository<SessionAnalytics>
    {
        Task<SessionAnalytics?> GetByGameSessionIdAsync(string gameSessionId);
        Task<IEnumerable<SessionAnalytics>> GetByGameIdAsync(string gameId);
        Task<IEnumerable<SessionAnalytics>> GetCompletedSessionsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
    
    public interface IPlayerAnalyticsRepository : IGenericRepository<PlayerAnalytics>
    {
        Task<IEnumerable<PlayerAnalytics>> GetBySessionAnalyticsIdAsync(string sessionAnalyticsId);
        Task<IEnumerable<PlayerAnalytics>> GetPlayerHistoryAsync(string playerName);
    }
}
