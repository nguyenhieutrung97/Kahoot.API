using BDKahoot.Application.Games.Dtos;
using BDKahoot.Domain.Enums;
using MediatR;

namespace BDKahoot.Application.Games.Queries.GetAllGames
{
    public class GetAllGamesQuery : IRequest<IEnumerable<GameDto>>
    {
        public string UserNTID { get; set; } = string.Empty;

        // Pagination properties
        public int? Skip { get; set; }
        public int? Take { get; set; }

        // Search term for filtering games
        public string? SearchTerm { get; set; }

        // Filter property 
        public GameState? StateFilter { get; set; }

        // Sorting properties
        public string? SortBy { get; set; }
        public SortDirection SortDirection { get; set; }
    }
}
