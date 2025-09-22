using BDKahoot.Application.Games.Commands.DeleteGame;
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

namespace BDKahoot.Application.Questions.Commands.DeleteQuestion
{
    public class DeleteQuestionCommandHandler(ILogger<DeleteQuestionCommandHandler> logger, IUnitOfWork unitOfWork) : IRequestHandler<DeleteQuestionCommand>
    {
        public async Task Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting question with id: " + request.QuestionId);

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.GameId.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can delete the question.");
            }

            // Validate that the question exists and belongs to the game
            var question = await unitOfWork.Questions.GetByIdAsync(request.QuestionId) ?? throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            if (question.GameId != request.GameId)
            {
                throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            }

            // Mark question as deleted
            question.Deleted = true;
            question.DeletedOn = DateTime.UtcNow;
            await unitOfWork.Questions.UpdateAsync(question);

            // Get answers of the question for soft deletion
            var answers = await unitOfWork.Answers.GetAnswerByQuestionID(request.QuestionId);
            foreach (var answer in answers)
            {
                answer.Deleted = true;
                answer.DeletedOn = DateTime.UtcNow;
                await unitOfWork.Answers.UpdateAsync(answer);
            }
        }
    }
}
