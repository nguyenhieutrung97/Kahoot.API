using BDKahoot.Domain.Models;
using FluentValidation;

namespace BDKahoot.Application.Games.Commands.CreateGame
{
    public class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
    {
        public CreateGameCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Game title is required.")
                .MaximumLength(100).WithMessage("Game title cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Game description cannot exceed 500 characters.");
        }
    }
}