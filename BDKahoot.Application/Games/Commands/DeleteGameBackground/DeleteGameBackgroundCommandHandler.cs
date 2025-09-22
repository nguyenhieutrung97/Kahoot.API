using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Games.Commands.DeleteGameBackground
{
    public class DeleteGameBackgroundCommandHandler(ILogger<DeleteGameBackgroundCommandHandler> logger, IUnitOfWork unitOfWork, IBlobStorageService blobStorageService) : IRequestHandler<DeleteGameBackgroundCommand>
    {
        public async Task Handle(DeleteGameBackgroundCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Deleting background image of game with Id: {request.Id}");

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can delete the background image of game.");
            }

            // Delete file from blob storage
            try
            {
                await blobStorageService.DeleteFileAsync(request.Id);
                logger.LogInformation($"Successfully deleted background image for game {request.Id}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error deleting background image for game {request.Id}");
                throw new InvalidOperationException($"Failed to delete background image for game {request.Id}", ex);
            }
        }
    }
}