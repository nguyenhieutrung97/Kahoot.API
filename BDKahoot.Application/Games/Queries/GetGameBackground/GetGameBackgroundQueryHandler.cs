using AutoMapper;
using BDKahoot.Application.Games.Queries.GetGameById;
using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Games.Queries.GetGameBackground
{
    public class GetGameBackgroundQueryHandler(ILogger<GetGameBackgroundQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork, IBlobStorageService blobStorageService) : IRequestHandler<GetGameBackgroundQuery, MemoryStream>
    {
        public async Task<MemoryStream> Handle(GetGameBackgroundQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting game background with Id: " + request.Id);

            // Validate that the game exists and the user is the owner
            Game game = await unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can get the game background.");
            }

            string blobName = request.Id;

            MemoryStream? fileStream = await blobStorageService.GetFileAsync(blobName);

            if (fileStream == null)
            {
                throw new FileNotFoundException($"Background for game {request.Id} not found.");
            }

            return fileStream;
        }
    }
}