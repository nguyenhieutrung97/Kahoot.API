using AutoMapper;
using BDKahoot.Application.Answers.Dtos;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Application.Questions.Queries.GetQuestionById;
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

namespace BDKahoot.Application.Answers.Queries.GetAnswerById
{
    public class GetAnswerByIdQueryHandler(ILogger<GetAnswerByIdQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<GetAnswerByIdQuery, AnswerDto>
    {
        public async Task<AnswerDto> Handle(GetAnswerByIdQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting answer with QuestionId: " + request.QuestionId);

            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.GameId.ToString());
            var question = await unitOfWork.Questions.GetByIdAsync(request.QuestionId) ?? throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());

            // Validate whether the current user is the owner of the game or not
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can view this question.");
            }

            // Validate whether the question exists and belongs to the game or not
            if (question.GameId != request.GameId)
            {
                throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            }

            // Validate whether the answer exists and belongs to the question or not
            if (question.Id != request.QuestionId) {
                throw new NotFoundExceptionCustom(nameof(Answer), request.AnswerId.ToString());
            }

            Answer answer = await unitOfWork.Answers.GetByIdAsync(request.AnswerId) ?? throw new NotFoundExceptionCustom(nameof(Answer), request.AnswerId.ToString());
            var answerDto = mapper.Map<AnswerDto>(answer);
            return answerDto;
        }
    }
}
