using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Dtos
{
    public class QuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public QuestionType Type { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
