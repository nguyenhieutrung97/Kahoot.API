using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Commands.UpdateQuestion
{
    public class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
    {
        public UpdateQuestionCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

            RuleFor(x => x.TimeLimitSeconds)
                .GreaterThan(0).WithMessage("Time limit must be greater than 0 seconds.")
                .LessThanOrEqualTo(30).WithMessage("Time limit must not exceed 30 seconds.");
        }
    }
}
