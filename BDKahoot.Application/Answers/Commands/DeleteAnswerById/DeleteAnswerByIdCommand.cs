using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Answers.Commands.DeleteAnswerById
{
    public class DeleteAnswerByIdCommand(string gameId, string questionId, string answerId, string ntid) : IRequest
    {
        public string GameId { get; set; } = gameId;
        public string QuestionId { get; set; } = questionId;
        public string AnswerId { get; set; } = answerId;
        public string UserNTID { get; set; } = ntid;
    }
}
