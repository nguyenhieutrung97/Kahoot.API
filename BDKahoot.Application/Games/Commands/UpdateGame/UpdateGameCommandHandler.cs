using AutoMapper;
using BDKahoot.Application.Games.Dtos;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Games.Commands.UpdateGame
{
    public class UpdateGameCommandHandler(ILogger<UpdateGameCommandHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<UpdateGameCommand>
    {
        public async Task Handle(UpdateGameCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Updating game with Id: {request.Id}");

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can update the game.");
            }

            // Update the game properties
            mapper.Map(request, game);
            game.UpdatedOn = DateTime.UtcNow;
            await unitOfWork.Games.UpdateAsync(game);
        }
    }
}