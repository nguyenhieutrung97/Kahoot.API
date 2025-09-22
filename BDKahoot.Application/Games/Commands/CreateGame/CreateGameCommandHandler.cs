using AutoMapper;
using BDKahoot.Application.Extensions;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BDKahoot.Application.Games.Commands.CreateGame
{
    public class CreateGameCommandHandler(ILogger<CreateGameCommandHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<CreateGameCommand, string>
    {
        public async Task<string> Handle(CreateGameCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"User {request.UserNTID} is creating game: {request.Title}");
            var game = mapper.Map<Game>(request);
            game.State = GameState.Draft; 
            var id = await unitOfWork.Games.AddAsync(game);

            return id;
        }
    }
}
