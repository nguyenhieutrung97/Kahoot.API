using BDKahoot.Application.Games.Commands.UploadGameBackground;
using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Games.Commands.UploadGameBackground
{
    public class UploadGameBackgroundCommandHandler(ILogger<UploadGameBackgroundCommandHandler> logger, IUnitOfWork unitOfWork, IBlobStorageService blobStorageService) : IRequestHandler<UploadGameBackgroundCommand>
    {
        public async Task Handle(UploadGameBackgroundCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Uploading background image of game with Id: {request.Id}");

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can upload the background image of game.");
            }

            // Upload file to blob storage and get the file URL
            await blobStorageService.UploadFileAsync(request.Id, request.File.OpenReadStream());
        }
    }
}
