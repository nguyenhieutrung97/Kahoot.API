using MediatR;

namespace BDKahoot.Application.Games.Commands.UpdateGame
{
    public class UpdateGameCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserNTID { get; set; } = string.Empty;
    }
}
