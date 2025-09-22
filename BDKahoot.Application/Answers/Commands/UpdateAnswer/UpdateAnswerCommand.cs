using BDKahoot.Application.Answers.Dtos;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Answers.Commands.UpdateAnswer
{
    public class UpdateAnswerCommand : IRequest<string>
    {
        public string GameId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public string UserNTID { get; set; } = string.Empty;
        public List<Answer> Answers { get; set; } = new List<Answer>();
    }
}
