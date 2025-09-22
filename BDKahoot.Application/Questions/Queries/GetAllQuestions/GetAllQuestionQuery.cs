using BDKahoot.Application.Questions.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Queries.GetAllQuestions
{
    public class GetAllQuestionQuery(string gameId, string userNTID) : IRequest<IEnumerable<QuestionDto>>
    {
        public string GameId { get; set; } = gameId;
        public string UserNTID { get; set; } = userNTID;
    }
}
