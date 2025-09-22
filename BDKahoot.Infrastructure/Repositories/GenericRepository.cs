using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;
using System.Linq.Expressions;
using MongoDB.Bson;

namespace BDKahoot.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;
        protected IMongoCollection<T> Collection => _collection;

        public GenericRepository(MongoDbContext context, string collectionName)
        {
            _collection = context.GetCollection<T>(collectionName);
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id) & Builders<T>.Filter.Eq("Deleted", false);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            var filter = Builders<T>.Filter.Eq("Deleted", false);
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<string> AddAsync(T entity)
        {
            // Generate ObjectId if not provided
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var currentId = (string?)idProperty.GetValue(entity);
                if (string.IsNullOrEmpty(currentId))
                {
                    var newId = ObjectId.GenerateNewId().ToString();
                    idProperty.SetValue(entity, newId);
                }
            }

            await _collection.InsertOneAsync(entity);
            return (string)(idProperty?.GetValue(entity) ?? "");
        }

        public async Task UpdateAsync(T entity)
        {
            var filter = Builders<T>.Filter.Eq("Id", (entity as dynamic).Id);
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("Id", id);
            await _collection.DeleteOneAsync(filter);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _collection.Find(predicate).AnyAsync();
        }
    }
}
