using BDKahoot.Application.Games.Dtos;
using MediatR;

namespace BDKahoot.Application.Games.Queries.GetGameById
{
    public class GetGameByIdQuery() : IRequest<GameDto>
    {
        public string Id { get; set; } = default!;
        public string UserNTID { get; set; } = default!;
    }
}