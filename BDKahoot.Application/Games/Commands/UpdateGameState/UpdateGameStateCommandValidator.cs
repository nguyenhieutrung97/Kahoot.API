using FluentValidation;
using static BDKahoot.Domain.Enums.GameState;

namespace BDKahoot.Application.Games.Commands.UpdateGameState
{
    public class UpdateGameStateCommandValidator : AbstractValidator<UpdateGameStateCommand>
    {
        public UpdateGameStateCommandValidator()
        {
            // Case: Draft can switch to Active, InActive.
            When(x => x.CurrentState == Draft, () =>
            {
                RuleFor(x => x.TargetState)
                    .Must(s => s == Active || s == InActive)
                    .WithMessage("For games in Draft, allowed transitions are only to Active or InActive.");
            });

            // Case: Active can switch to Draft, InLobby, InActive.
            When(x => x.CurrentState == Active, () =>
            {
                RuleFor(x => x.TargetState)
                    .Must(s => s == Draft || s == InLobby || s == InActive)
                    .WithMessage("For games in Active, allowed transitions are Draft, InLobby, or InActive.");
            });

            // Case: InLobby can start game.
            When(x => x.CurrentState == InLobby, () =>
            {
                RuleFor(x => x.TargetState)
                    .Equal(Active)
                    .WithMessage("A game in InLobby can only be started to Active.");
            });

            // Case: InActive can switch to Active.
            When(x => x.CurrentState == InActive, () =>
            {
                RuleFor(x => x.TargetState)
                    .Equal(Active)
                    .WithMessage("For games in InActive, the only allowed transition is to Active.");
            });
        }
    }
}
