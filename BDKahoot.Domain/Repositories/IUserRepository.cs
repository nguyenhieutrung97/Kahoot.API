using BDKahoot.Domain.Models;

namespace BDKahoot.Domain.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUpnAsync(string upn);
    }
}
