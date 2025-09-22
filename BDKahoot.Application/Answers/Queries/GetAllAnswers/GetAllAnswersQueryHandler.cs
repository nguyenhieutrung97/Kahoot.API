using AutoMapper;
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

namespace BDKahoot.Application.Answers.Queries.GetAllAnswers
{
    public class GetAllAnswersQueryHandler(ILogger<GetAllAnswersQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<GetAllAnswersQuery, IEnumerable<AnswerDto>>
    {
        public async Task<IEnumerable<AnswerDto>> Handle(GetAllAnswersQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Getting all answer for question with Id: {request.QuestionId}");

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

            var answers = await unitOfWork.Answers.GetAnswerByQuestionID(request.QuestionId);
            var answersDtos = mapper.Map<IEnumerable<AnswerDto>>(answers);
            return answersDtos;
        }
    }
}
