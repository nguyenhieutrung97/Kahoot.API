using BDKahoot.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Commands.CreateQuestion
{
    public class CreateQuestionCommand : IRequest<string>
    {
        public string GameId { get; set; } = string.Empty;
        public string UserNTID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public QuestionType Type { get; set; }
    }
}
