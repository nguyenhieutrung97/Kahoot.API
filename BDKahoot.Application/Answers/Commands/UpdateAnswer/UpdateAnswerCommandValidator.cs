using BDKahoot.Application.Answers.Commands.CreateAnswer;
using BDKahoot.Domain.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Answers.Commands.UpdateAnswer
{
    public class UpdateAnswerCommandValidator : AbstractValidator<UpdateAnswerCommand>
    {
        private static readonly QuestionType[] allowedTypes = (QuestionType[])Enum.GetValues(typeof(QuestionType));
        public UpdateAnswerCommandValidator()
        {
            // Single choice
            When(a => a.QuestionType == QuestionType.SingleChoice, () =>
            {
                RuleFor(a => a.Answers)
                    .Must(answer => answer.Count(a => a.IsCorrect == true) == 1)
                    .WithMessage("Single Choice - Accept only one correct answer.");
            });

            // Multiple choice
            When(a => a.QuestionType == QuestionType.MultipleChoice, () =>
            {
                RuleFor(a => a.Answers)
                    .Must(answer => answer.Count(a => a.IsCorrect == true) >= 1)
                    .WithMessage("Multiple Choice - Must have at least one correct answer.");
            });

            // True False
            When(a => a.QuestionType == QuestionType.TrueFalse, () =>
            {
                RuleFor(a => a.Answers)
                    .Must(answer => answer.Count(a => a.IsCorrect == true) == 1)
                    .WithMessage("True/False - Accept only one correct answer.");
            });

            // Validate Title of each answer
            RuleForEach(a => a.Answers).ChildRules(answer =>
            {
                answer.RuleFor(ans => ans.Title)
                    .NotEmpty().WithMessage("Title is required.")
                    .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");
            });

            // Validate QuestionType
            RuleFor(a => a.QuestionType)
                .IsInEnum().WithMessage("Invalid question type specified.")
                .Must(type => allowedTypes.Contains(type))
                    .WithMessage($"Question type must be one of the following: {string.Join(", ", allowedTypes)}."); ;
        }
    }
}
