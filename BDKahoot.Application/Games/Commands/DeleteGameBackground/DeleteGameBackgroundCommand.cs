using MediatR;

namespace BDKahoot.Application.Games.Commands.DeleteGameBackground
{
    public class DeleteGameBackgroundCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string UserNTID { get; set; } = string.Empty;
    }
}       