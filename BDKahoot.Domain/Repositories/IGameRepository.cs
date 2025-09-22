using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Domain.Repositories
{
    public interface IGameRepository : IGenericRepository<Game>
    {
        Task<Game?> GetByTitleAsync(string title);

        Task<IEnumerable<Game>> GetGamesByHostUserNTIDAsync(string hostUserNtid);
        Task<List<Game>> GetByCreatedOnRangeAsync(DateTime startUtc, DateTime endUtc);
    }
}
