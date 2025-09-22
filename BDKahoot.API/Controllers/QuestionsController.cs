using BDKahoot.Application.Extensions;
using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Application.Questions.Commands.DeleteQuestion;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Application.Questions.Queries.GetAllQuestions;
using BDKahoot.Application.Questions.Queries.GetQuestionById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDKahoot.API.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/games/{gameId}/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public QuestionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionsAsync(string gameId)
        {
            var ntid = User.GetUserNTID();
            var query = new GetAllQuestionQuery(gameId, ntid);
            var questions = await _mediator.Send(query);
            return Ok(questions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionByIdAsync(string gameId, string id)
        {
            var ntid = User.GetUserNTID();
            var query = new GetQuestionByIdQuery(gameId, id, ntid);
            var question = await _mediator.Send(query);
            return Ok(question);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestionAsync(string gameId, [FromBody] CreateQuestionCommand command)
        {
            var ntid = User.GetUserNTID();
            command.GameId = gameId;
            command.UserNTID = ntid;
            var id = await _mediator.Send(command);
            return Created($"/api/games/{gameId}/questions/{id}", null);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateQuestionAsync(string gameId, string id, [FromBody] UpdateQuestionCommand updateQuestionCommand)
        {
            var ntid = User.GetUserNTID();
            updateQuestionCommand.UserNTID = ntid;
            updateQuestionCommand.QuestionId = id;
            updateQuestionCommand.GameId = gameId;
            await _mediator.Send(updateQuestionCommand);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestionAsync(string gameId, string id)
        {
            var ntid = User.GetUserNTID();
            var command = new DeleteQuestionCommand(gameId, id, ntid);
            await _mediator.Send(command);
            return NoContent();
        }
    }
}
