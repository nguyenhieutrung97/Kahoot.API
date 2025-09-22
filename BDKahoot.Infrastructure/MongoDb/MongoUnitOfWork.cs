using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.Repositories;

namespace BDKahoot.Infrastructure.MongoDb
{

    public class MongoUnitOfWork : IUnitOfWork
    {
        private readonly MongoDbContext _context;
        
        public IGameRepository Games { get; }
        public IUserRepository Users { get; }
        public IQuestionRepository Questions { get; }
        public IAnswerRepository Answers { get; }
        public IGameSessionRepository GameSessions { get; }
        public IPlayerRepository Players { get; }
        public IAnalyticsRepositories Analytics { get; }
        public ISessionAnalyticsRepository SessionAnalytics { get; }
        public IPlayerAnalyticsRepository PlayerAnalytics { get; }

        public MongoUnitOfWork(MongoDbContext context)
        {
            _context = context;
            Games = new GameRepository(context);
            Users = new UserRepository(context);
            Questions = new QuestionRepository(context);
            Answers = new AnswerRepository(context);
            GameSessions = new GameSessionRepository(context);
            Players = new PlayerRepository(context);
            Analytics = new AnalyticsRepository(context);
            SessionAnalytics = new SessionAnalyticsRepository(context);
            PlayerAnalytics = new PlayerAnalyticsRepository(context);
        }
    }
}
