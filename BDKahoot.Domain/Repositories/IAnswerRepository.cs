using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Domain.Repositories
{
    public interface IAnswerRepository : IGenericRepository<Answer>
    {
        Task<IEnumerable<Answer>> GetAnswerByQuestionID(string questionId);
    }
}
