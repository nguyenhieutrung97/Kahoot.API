using BDKahoot.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Questions.Commands.CreateQuestion
{
    public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
    {
        private static readonly QuestionType[] allowedTypes = (QuestionType[])Enum.GetValues(typeof(QuestionType));

        public CreateQuestionCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

            RuleFor(x => x.TimeLimitSeconds)
                .GreaterThan(0).WithMessage("Time limit must be greater than 0 seconds.")
                .LessThanOrEqualTo(30).WithMessage("Time limit must not exceed 30 seconds.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid question type specified.")
                .Must(type => allowedTypes.Contains(type))
                    .WithMessage($"Question type must be one of the following: {string.Join(", ", allowedTypes)}.");
        }
    }
}
