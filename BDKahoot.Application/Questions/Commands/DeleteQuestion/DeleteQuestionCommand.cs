using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Commands.DeleteQuestion
{
    public class DeleteQuestionCommand(string gameId, string questionId, string userNTID) : IRequest
    {
        public string GameId { get; set; } = gameId;
        public string QuestionId { get; set; } = questionId;
        public string UserNTID { get; set; } = userNTID;
    }
}
