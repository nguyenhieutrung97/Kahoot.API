using AutoMapper;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Questions.Queries.GetAllQuestions
{
    public class GetAllQuestionQueryHandler(ILogger<GetAllQuestionQueryHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<GetAllQuestionQuery, IEnumerable<QuestionDto>>
    {
        public async Task<IEnumerable<QuestionDto>> Handle(GetAllQuestionQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Getting all questions for game with Id: {request.GameId}");

            // Validate that the game exists and the user is the owner
            var game = await unitOfWork.Games.GetByIdAsync(request.GameId) ?? throw new NotFoundExceptionCustom(nameof(Game), request.GameId.ToString());
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can view questions of this game.");
            }

            var questions = await unitOfWork.Questions.GetQuestionsByGameIdAsync(request.GameId);
            var questionsDtos = mapper.Map<IEnumerable<QuestionDto>>(questions);
            return questionsDtos;
        }
    }
}
