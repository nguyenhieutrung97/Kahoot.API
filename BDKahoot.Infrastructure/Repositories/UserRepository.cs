using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly IMongoCollection<User> _userCollection;

        public UserRepository(MongoDbContext context) : base(context, "Users")
        {
            _userCollection = context.GetCollection<User>("Users");
        }

        public async Task<User?> GetByUpnAsync(string upn)
        {
            return await _userCollection.Find(g => g.Upn == upn).FirstOrDefaultAsync();
        }
    }
}
