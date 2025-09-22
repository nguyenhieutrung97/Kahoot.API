using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Services.AnalyticsService
{
    public interface IAnalyticsService
    {
        Task RecordGameAnalyticDaily(DateTime date);
        Task<GameAnalytics> GetDailyAnalyticsAsync(DateTime date);
        Task<IEnumerable<GameAnalytics>> GetAnalyticsRangeAsync(DateTime startDate, DateTime endDate);
        Task<SessionAnalytics> CreateSessionAnalyticsAsync(GameSession session, IEnumerable<Player> players);
        Task<IEnumerable<SessionAnalytics>> GetGameSessionHistoryAsync(string gameId);
        Task<IEnumerable<PlayerAnalytics>> GetPlayerHistoryAsync(string playerName);
    }
}
