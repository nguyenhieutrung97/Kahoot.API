using AutoMapper;
using BDKahoot.Application.Games.Dtos;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Games.Queries.GetGameById
{
    public class GetGameByIdQueryHandler(ILogger<GetGameByIdQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<GetGameByIdQuery, GameDto>
    {
        public async Task<GameDto> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting game with Id: " + request.Id);

            // Validate that the game exists and the user is the owner
            Game game = await unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can view the game.");
            }

            var gameDto = mapper.Map<GameDto>(game);
            return gameDto;
        }
    }
}
