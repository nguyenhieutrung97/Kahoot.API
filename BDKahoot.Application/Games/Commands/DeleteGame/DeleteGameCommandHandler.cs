using BDKahoot.Application.Games.Queries.GetGameById;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Games.Commands.DeleteGame
{
    public class DeleteGameCommandHandler(ILogger<DeleteGameCommandHandler> logger, IUnitOfWork unitOfWork) : IRequestHandler<DeleteGameCommand>
    {
        public async Task Handle(DeleteGameCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting game with id: " + request.Id);

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.Id) ?? throw new NotFoundExceptionCustom(nameof(Game), request.Id.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can delete the game.");
            }

            // Mark game as deleted
            game.Deleted = true;
            game.DeletedOn = DateTime.UtcNow;
            await unitOfWork.Games.UpdateAsync(game);

            // Also delete related questions and answers
            var questions = await unitOfWork.Questions.GetQuestionsByGameIdAsync(request.Id);
            foreach (var question in questions)
            {
                question.Deleted = true;
                question.DeletedOn = DateTime.UtcNow;
                await unitOfWork.Questions.UpdateAsync(question);

                var answers = await unitOfWork.Answers.GetAnswerByQuestionID(question.Id);
                foreach (var answer in answers)
                {
                    answer.Deleted = true;
                    answer.DeletedOn = DateTime.UtcNow;
                    await unitOfWork.Answers.UpdateAsync(answer);
                }
            }
        }
    }
}
