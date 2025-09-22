using AutoMapper;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Questions.Commands.CreateQuestion
{
    public class CreateQuestionCommandHandler(ILogger<CreateQuestionCommandHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<CreateQuestionCommand, string>
    {

        public async Task<string> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"User {request.UserNTID} is creating a question: {request.Title} for the game {request.GameId}.");

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.GameId.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can update the game.");
            }
            var question = mapper.Map<Question>(request);
            var id = await unitOfWork.Questions.AddAsync(question);

            return id;
        }
    }
}
