using BDKahoot.Domain.Enums;
using MediatR;

namespace BDKahoot.Application.Games.Commands.UpdateGameState
{
    public class UpdateGameStateCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string UserNTID { get; set; } = string.Empty;
        public GameState CurrentState { get; set; }
        public GameState TargetState { get; set; }
    }
}
