using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        private readonly IMongoCollection<Question> _questionCollection;

        public QuestionRepository(MongoDbContext context) : base(context, "Questions")
        {
            _questionCollection = context.GetCollection<Question>("Questions");
        }

        public async Task<IEnumerable<Question>> GetQuestionsByGameIdAsync(string gameId)
        {
            return await _questionCollection.Find(q => q.GameId == gameId && !q.Deleted).ToListAsync();
        }
    }
}
