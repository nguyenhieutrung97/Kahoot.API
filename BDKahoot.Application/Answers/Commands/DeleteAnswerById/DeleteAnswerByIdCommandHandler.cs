using AutoMapper;
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

namespace BDKahoot.Application.Answers.Commands.DeleteAnswerById
{
    public class DeleteAnswerByIdCommandHandler(ILogger<DeleteAnswerByIdCommandHandler> logger, IUnitOfWork unitOfWork) : IRequestHandler<DeleteAnswerByIdCommand>
    {
        public async Task Handle(DeleteAnswerByIdCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Deleting answer with id: " + request.AnswerId);

            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.QuestionId.ToString());
            var question = await unitOfWork.Questions.GetByIdAsync(request.QuestionId) ?? throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());


            // Validate whether the current user is the owner of the game or not
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can delete answers for question");
            }

            // Validate whether the answer for the question is belong to the game or not
            if (game.Id != question.GameId)
            {
                throw new UnauthorizedAccessExceptionCustom("The question belongs to another game, please double-check");
            }

            await unitOfWork.Answers.DeleteAsync(request.AnswerId);
        }
    }
}
