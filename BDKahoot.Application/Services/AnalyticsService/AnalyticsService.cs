using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Services.AnalyticsService
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IUnitOfWork unitOfWork, ILogger<AnalyticsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RecordGameAnalyticDaily(DateTime date)
        {
            var games = new List<Game>();
            var gameSessions = new List<GameSession>();
            var sessionAnalytics = new List<SessionAnalytics>();
            var gameAnalytics = new GameAnalytics();

            // Treat the input date as a calendar date in UTC
            var startUtc = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var endUtc = startUtc.AddDays(1);

            // Get all games created on that date
            games = await _unitOfWork.Games.GetByCreatedOnRangeAsync(startUtc, endUtc);
            _logger.LogInformation($"Found {games.Count} games created on {startUtc:yyyy-MM-dd}");

            // Aggregate related data for all games (use AddRange instead of overwriting)
            foreach (var game in games)
            {
                var sessions = (await _unitOfWork.GameSessions.GetByGameIdAsync(game.Id)).ToList();
                gameSessions.AddRange(sessions);
                _logger.LogInformation($"Game {game.Id} has {sessions.Count} sessions");

                var sa = (await _unitOfWork.SessionAnalytics.GetByGameIdAsync(game.Id)).ToList();
                sessionAnalytics.AddRange(sa);
                _logger.LogInformation($"Game {game.Id} has {sa.Count} session analytics entries");
            }

            // Set analytics fields
            gameAnalytics.Date = startUtc.Date;
            gameAnalytics.CreatedOn = DateTime.UtcNow;

            // RecordGameCreatedDailyAsync
            gameAnalytics.GamesCreated = games.Count;
            _logger.LogInformation($"Total games created: {gameAnalytics.GamesCreated}");

            // RecordGameSessionStartedDailyAsync
            gameAnalytics.GameSessionsStarted = gameSessions.Count;
            _logger.LogInformation($"Total game sessions started: {gameAnalytics.GameSessionsStarted}");

            // RecordPlayerJoinedDailyAsync
            gameAnalytics.PlayersJoined = sessionAnalytics.Sum(s => s.PlayersJoined);
            _logger.LogInformation($"Total players joined across all sessions: {gameAnalytics.PlayersJoined}");

            // RecordGameSessionCompletedDailyAsync
            gameAnalytics.GameSessionsCompleted = gameSessions.Count(gs => gs.State == Domain.Enums.GameSessionState.Completed);
            _logger.LogInformation($"Total game sessions completed: {gameAnalytics.GameSessionsCompleted}");

            // RecordGameSessionAbortedDailyAsync
            gameAnalytics.GameSessionsAborted = gameSessions.Count(gs => gs.State == Domain.Enums.GameSessionState.Aborted);
            _logger.LogInformation($"Total game sessions aborted: {gameAnalytics.GameSessionsAborted}");

            // RecordGameSessionAverage — sum durations safely using TotalMinutes
            var totalDurationMinutes = sessionAnalytics.Sum(s => s.Duration?.TotalMinutes ?? 0d);
            gameAnalytics.UpdateAverages(gameAnalytics.GameSessionsStarted, totalDurationMinutes);
            _logger.LogInformation($"Average players per session: {gameAnalytics.AveragePlayersPerSession}");

            // Save to database
            await _unitOfWork.Analytics.AddAsync(gameAnalytics);
            _logger.LogInformation($"Recorded daily game analytics for {startUtc:yyyy-MM-dd} and saved to MongoDB successfully");
        }

        public async Task<GameAnalytics> GetDailyAnalyticsAsync(DateTime date)
        {
            var analytics = await _unitOfWork.Analytics.GetByDateAsync(date.Date);
            return analytics ?? new GameAnalytics { Date = date.Date };
        }

        public async Task<IEnumerable<GameAnalytics>> GetAnalyticsRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Analytics.GetDateRangeAsync(startDate.Date, endDate.Date);
        }

        public async Task<SessionAnalytics> CreateSessionAnalyticsAsync(GameSession session, IEnumerable<Player> players)
        {
            try
            {
                var playersList = players.ToList();
                var winner = playersList.OrderByDescending(p => p.Score).FirstOrDefault();
                
                _logger.LogInformation($"Creating session analytics for session {session.Id} with {playersList.Count} players");
                _logger.LogInformation($"Players data: {string.Join(", ", playersList.Select(p => $"{p.UserName}:{p.Score}:{p.CorrectAnswers}/{p.TotalAnswers}"))}");
                
                var sessionAnalytics = new SessionAnalytics
                {
                    GameSessionId = session.Id ?? string.Empty,
                    GameId = session.GameId,
                    RoomCode = session.RoomCode,
                    StartTime = session.StartOn,
                    EndTime = session.EndOn,
                    PlayersJoined = playersList.Count,
                    QuestionsAnswered = playersList.Sum(p => p.TotalAnswers),
                    TotalQuestions = session.TotalQuestions,
                    WinnerName = winner?.UserName ?? "",
                    WinnerScore = winner?.Score ?? 0,
                    AverageScore = playersList.Any() ? playersList.Average(p => p.Score) : 0,
                    CreatedOn = DateTime.UtcNow
                };

                // Create player analytics
                var playerAnalytics = playersList.Select((player, index) => new PlayerAnalytics
                {
                    SessionAnalyticsId = sessionAnalytics.Id ?? string.Empty,
                    PlayerId = player.Id ?? string.Empty,
                    PlayerName = player.UserName,
                    FinalScore = player.Score,
                    CorrectAnswers = player.CorrectAnswers,
                    TotalAnswers = player.TotalAnswers,
                    AccuracyPercentage = player.AccuracyPercentage,
                    AverageResponseTime = player.AverageResponseTime, // Include actual response time data
                    FinalRank = index + 1,
                    JoinTime = player.JoinedAt,
                    PlayDuration = session.EndOn?.Subtract(player.JoinedAt),
                    CreatedOn = DateTime.UtcNow
                }).ToList();

                sessionAnalytics.PlayerStats = playerAnalytics;
                
                _logger.LogInformation($"Created {playerAnalytics.Count} player analytics entries for session {session.Id}");

                // Save to database
                await _unitOfWork.SessionAnalytics.AddAsync(sessionAnalytics);
                
                foreach (var playerAnalytic in playerAnalytics)
                {
                    await _unitOfWork.PlayerAnalytics.AddAsync(playerAnalytic);
                }

                _logger.LogInformation($"Created session analytics for session {session.Id} and saved to MongoDB successfully");
                
                return sessionAnalytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating session analytics for session {session.Id}");
                throw;
            }
        }

        public async Task<IEnumerable<SessionAnalytics>> GetGameSessionHistoryAsync(string gameId)
        {
            return await _unitOfWork.SessionAnalytics.GetByGameIdAsync(gameId);
        }

        public async Task<IEnumerable<PlayerAnalytics>> GetPlayerHistoryAsync(string playerName)
        {
            return await _unitOfWork.PlayerAnalytics.GetPlayerHistoryAsync(playerName);
        }
    }
}
