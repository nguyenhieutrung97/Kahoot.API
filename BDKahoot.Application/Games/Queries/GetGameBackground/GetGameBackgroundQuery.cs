using MediatR;

namespace BDKahoot.Application.Games.Queries.GetGameBackground
{
    public class GetGameBackgroundQuery : IRequest<MemoryStream>
    {
        public string Id { get; set; } = string.Empty;
        public string UserNTID { get; set; } = string.Empty;
    }
}