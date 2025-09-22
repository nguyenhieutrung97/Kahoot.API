using FluentValidation;

namespace BDKahoot.Application.Games.Commands.UpdateGame
{
    public class UpdateGameCommandValidator : AbstractValidator<UpdateGameCommand>
    {
        public UpdateGameCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Game title is required.")
                .MaximumLength(100).WithMessage("Game title cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Game description cannot exceed 500 characters.");
        }
    }
}
