using System.Linq.Expressions;

namespace BDKahoot.Domain.Repositories
{

    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<string> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}
