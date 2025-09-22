using BDKahoot.Application.Questions.Dtos;
using MediatR;

namespace BDKahoot.Application.Questions.Queries.GetQuestionById
{
    public class GetQuestionByIdQuery(string gameId, string questionid, string userNTID) : IRequest<QuestionDto>
    {
        public string GameId { get; set; } = gameId;
        public string QuestionId { get; set; } = questionid;
        public string UserNTID { get; set; } = userNTID;
    }
}
