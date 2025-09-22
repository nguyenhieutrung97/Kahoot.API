using AutoMapper;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BDKahoot.Application.Answers.Commands.CreateAnswer
{
    public class CreateAnswerCommandHandler(ILogger<CreateAnswerCommandHandler> logger, IMapper mapper, IUnitOfWork unitOfWork) : IRequestHandler<CreateAnswerCommand, string>
    {
        public async Task<string> Handle(CreateAnswerCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Create answer for a question ID: ${request.QuestionId}");

            string listId = string.Empty;
            var game = await unitOfWork.Games.GetByIdAsync( request.GameId ) ?? throw new NotFoundExceptionCustom(nameof(Game), request.QuestionId.ToString());
            var question = await unitOfWork.Questions.GetByIdAsync( request.QuestionId ) ?? throw new NotFoundExceptionCustom(nameof(Question), request.QuestionId.ToString()) ;

            // Validate whether the current user is the owner of the game or not
            if (game.HostUserNTID != request.UserNTID)
            {
                throw new UnauthorizedAccessExceptionCustom("Only the owner can create answers for question");
            }

            // Validate whether the answer for the question is belong to the game or not
            if (game.Id != question.GameId)
            {
                throw new UnauthorizedAccessExceptionCustom("The question belongs to another game, please double-check");
            }
            
            foreach (var a in request.Answers)
            {
                Answer newAnswer = new Answer();

                newAnswer.GameId = request.GameId;
                newAnswer.QuestionId = request.QuestionId;
                newAnswer.Title = a.Title;
                newAnswer.IsCorrect = a.IsCorrect;

                var mapAnswer = mapper.Map<Answer>(newAnswer);
                var id = await unitOfWork.Answers.AddAsync(mapAnswer);

                if (string.IsNullOrEmpty(listId)) {
                    listId = id;
                }
                else
                {
                    listId = listId + "/" + id;
                };
            }
            return listId.Trim();
        }
    }
}
