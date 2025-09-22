using AutoMapper;
using Azure.Core;
using BDKahoot.Application.Answers.Dtos;
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

namespace BDKahoot.Application.Answers.Commands.UpdateAnswer
{
    public class UpdateAnswerCommandHandler(ILogger<UpdateAnswerCommandHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<UpdateAnswerCommand, string>
    {
        public async Task<string> Handle(UpdateAnswerCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Updating answers with QuestionId: " + request.QuestionId);

            string listId = string.Empty;
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

            foreach (var a in request.Answers)
            {
                var answer = await unitOfWork.Answers.GetByIdAsync(a.Id) ?? throw new NotFoundExceptionCustom(nameof(Answer), a.Id.ToString());

                mapper.Map(answer, a);
                a.GameId = request.GameId;
                a.QuestionId = request.QuestionId;
                a.UpdatedOn = DateTime.UtcNow;

                await unitOfWork.Answers.UpdateAsync(a);

                // Test data update successful or not
                var updateAnswer = await unitOfWork.Answers.GetByIdAsync(a.Id) ?? throw new NotFoundExceptionCustom(nameof(Answer), a.Id.ToString());

                // Return string
                if (string.IsNullOrEmpty(listId))
                {
                    listId = a.Id;
                }
                else
                {
                    listId = listId + "/" + a.Id;
                };
            }

            return listId.Trim();
        }
    }
}