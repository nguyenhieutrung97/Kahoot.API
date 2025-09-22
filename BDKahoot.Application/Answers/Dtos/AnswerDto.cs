using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Answers.Dtos
{
    public class AnswerDto
    {
        public string ID { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public DateTime? DeletedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
