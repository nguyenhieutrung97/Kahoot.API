using BDKahoot.Application.Answers.Dtos;
using BDKahoot.Domain.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Answers.Queries.GetAnswerById
{
    public class GetAnswerByIdQuery(string gameId, string questionId, string answerId, string userNTID) : IRequest<AnswerDto>
    {
        public string GameId { get; set; } = gameId;
        public string QuestionId { get; set; } = questionId;
        public string AnswerId { get; set; } = answerId;
        public string UserNTID { get; set; } = userNTID;
    }
}
