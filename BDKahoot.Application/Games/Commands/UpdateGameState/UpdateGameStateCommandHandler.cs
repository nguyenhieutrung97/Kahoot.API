using BDKahoot.Application.Games.Commands.UpdateGame;
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

namespace BDKahoot.Application.Games.Commands.UpdateGameState
{
    public class UpdateGameStateCommandHandler(ILogger<UpdateGameCommandHandler> logger, IUnitOfWork _unitOfWork) : IRequestHandler<UpdateGameStateCommand>
    {
        public async Task Handle(UpdateGameStateCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Updating state of game with Id: {request.Id}");

            // Validate that the game exists and the user is the owner
            var game = await _unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can update the game state.");
            }

            // Update the game state
            game.State = request.TargetState;
            game.UpdatedOn = DateTime.UtcNow;
            await _unitOfWork.Games.UpdateAsync(game);
        }
    }
}
