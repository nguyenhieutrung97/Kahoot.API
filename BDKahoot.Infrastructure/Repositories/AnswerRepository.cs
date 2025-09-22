using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class AnswerRepository : GenericRepository<Answer>, IAnswerRepository
    {
        private readonly IMongoCollection<Answer> _answerCollection;

        public AnswerRepository(MongoDbContext context) : base(context, "Answers") {
            _answerCollection = context.GetCollection<Answer>("Answers");
        }
        
        public async Task<IEnumerable<Answer>> GetAnswerByQuestionID(string questionId)
        {
            return await _answerCollection.Find(a => a.QuestionId == questionId && !a.Deleted).ToListAsync();
        }
    }
}
