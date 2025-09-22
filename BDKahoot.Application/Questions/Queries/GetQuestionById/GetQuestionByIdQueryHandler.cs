using AutoMapper;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Application.Questions.Queries.GetAllQuestions;
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

namespace BDKahoot.Application.Questions.Queries.GetQuestionById
{
    public class GetQuestionByIdQueryHandler(ILogger<GetQuestionByIdQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<GetQuestionByIdQuery, QuestionDto>
    {
        public async Task<QuestionDto> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Getting question with Id: " + request.QuestionId);

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.GameId.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can view this question.");
            }

            // Validate that the question exists and belongs to the game
            Question question = await unitOfWork.Questions.GetByIdAsync(request.QuestionId) ?? throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            if (question.GameId != request.GameId)
            {
                throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString());
            }

            var questionDto = mapper.Map<QuestionDto>(question);
            return questionDto;
        }
    }
}
