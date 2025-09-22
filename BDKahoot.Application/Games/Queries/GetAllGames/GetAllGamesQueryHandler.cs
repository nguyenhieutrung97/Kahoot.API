using AutoMapper;
using BDKahoot.Application.Games.Dtos;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Games.Queries.GetAllGames
{
    public class GetAllGamesQueryHandler(ILogger<GetAllGamesQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<GetAllGamesQuery, IEnumerable<GameDto>>
    {
        public async Task<IEnumerable<GameDto>> Handle(GetAllGamesQuery request, CancellationToken cancellationToken)
        {
            // Get initial query of games for the host user
            var gamesOfHost = await unitOfWork.Games.GetGamesByHostUserNTIDAsync(request.UserNTID);

            // Apply pagination if provided
            if (request.Skip.HasValue)
            {
                gamesOfHost = gamesOfHost.Skip((int)request.Skip);
            }

            if (request.Take.HasValue)
            {
                gamesOfHost = gamesOfHost.Take((int)request.Take);
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower().Trim();
                gamesOfHost = gamesOfHost.Where(g =>
                    g.Title.ToLower().Contains(searchTerm) ||
                    g.Description.ToLower().Contains(searchTerm));
            }

            // Apply state filter if provided
            if (request.StateFilter.HasValue)
            {
                gamesOfHost = gamesOfHost.Where(g => g.State == request.StateFilter.Value);
            }

            // Apply sorting if provided
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                gamesOfHost = ApplySorting(gamesOfHost, request.SortBy.ToLower().Trim(), request.SortDirection);
            }

            var games = gamesOfHost.ToList();

            if (!games.Any())
            {
                logger.LogInformation("No games found for user {HostUserNTID}", request.UserNTID);
                return Enumerable.Empty<GameDto>();
            }
            var gamesDtos = mapper.Map<IEnumerable<GameDto>>(games);
            return gamesDtos;
        }

        private static IEnumerable<Game> ApplySorting(IEnumerable<Game> gamesOfHost, string sortBy, SortDirection sortDirection)
        {
            return sortBy.ToLower().Trim() switch
            {
                nameof(Game.Title) => sortDirection == SortDirection.Ascending
                    ? gamesOfHost.OrderBy(g => g.Title)
                    : gamesOfHost.OrderByDescending(g => g.Title),
                nameof(Game.CreatedOn) => sortDirection == SortDirection.Ascending
                    ? gamesOfHost.OrderBy(g => g.CreatedOn)
                    : gamesOfHost.OrderByDescending(g => g.CreatedOn),
                nameof(Game.UpdatedOn) => sortDirection == SortDirection.Ascending
                    ? gamesOfHost.OrderBy(g => g.UpdatedOn)
                    : gamesOfHost.OrderByDescending(g => g.UpdatedOn),
                nameof(Game.State) => sortDirection == SortDirection.Ascending
                    ? gamesOfHost.OrderBy(g => g.State)
                    : gamesOfHost.OrderByDescending(g => g.State),
                _ => sortDirection == SortDirection.Ascending
                    ? gamesOfHost.OrderBy(g => g.CreatedOn)
                    : gamesOfHost.OrderByDescending(g => g.CreatedOn)
            };
        }
    }
}
