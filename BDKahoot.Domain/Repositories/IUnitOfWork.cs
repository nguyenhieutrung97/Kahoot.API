namespace BDKahoot.Domain.Repositories
{
    public interface IUnitOfWork
    {
        IGameRepository Games { get; }
        IUserRepository Users { get; }
        IQuestionRepository Questions { get; }
        IAnswerRepository Answers { get; }
        IGameSessionRepository GameSessions { get; }
        IPlayerRepository Players { get; }
        IAnalyticsRepositories Analytics { get; }
        ISessionAnalyticsRepository SessionAnalytics { get; }
        IPlayerAnalyticsRepository PlayerAnalytics { get; }
    }
}
