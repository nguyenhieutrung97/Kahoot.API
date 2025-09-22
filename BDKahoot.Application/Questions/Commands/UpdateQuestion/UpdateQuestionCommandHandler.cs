using AutoMapper;
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

namespace BDKahoot.Application.Questions.Commands.UpdateQuestion
{
    public class UpdateQuestionCommandHandler(ILogger<UpdateGameCommandHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<UpdateQuestionCommand>
    {
        public async Task Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating question with id: " + request.QuestionId);

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.GameId.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can update the question.");
            }

            // Validate that the question exists and belongs to the game
            var question = await unitOfWork.Questions.GetByIdAsync(request.QuestionId) ?? throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            if (question.GameId != request.GameId)
            {
                throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            }

            // Update the question properties
            mapper.Map(request, question);
            question.UpdatedOn = DateTime.UtcNow;
            await unitOfWork.Questions.UpdateAsync(question);
        }
    }
}
