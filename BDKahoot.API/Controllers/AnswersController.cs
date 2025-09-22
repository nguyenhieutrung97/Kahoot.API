using BDKahoot.Application.Answers.Commands.CreateAnswer;
using BDKahoot.Application.Answers.Commands.DeleteAnswer;
using BDKahoot.Application.Answers.Commands.DeleteAnswerById;
using BDKahoot.Application.Answers.Commands.UpdateAnswer;
using BDKahoot.Application.Answers.Queries.GetAllAnswers;
using BDKahoot.Application.Answers.Queries.GetAnswerById;
using BDKahoot.Application.Extensions;
using BDKahoot.Application.Questions.Commands.DeleteQuestion;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Application.Questions.Queries.GetAllQuestions;
using BDKahoot.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BDKahoot.API.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/games/{gameId}/questions/{questionId}/[controller]")]
    public class AnswersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnswersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnswersAsync(string gameId, string questionId)
        {
            var ntid = User.GetUserNTID();
            var query = new GetAllAnswersQuery(gameId, questionId, ntid);
            var answers = await _mediator.Send(query);
            return Ok(answers);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetAnswerByIdAsync(string gameId, string questionId, string Id)
        {
            var ntid = User.GetUserNTID();
            var query = new GetAnswerByIdQuery(gameId, questionId, Id, ntid);
            var answers = await _mediator.Send(query);
            return Ok(answers);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAnswerAsync(string gameId, string questionId, [FromBody] CreateAnswerCommand command)
        {
            var ntid = User.GetUserNTID();
            command.GameId = gameId;
            command.QuestionId = questionId;
            command.UserNTID = ntid;
            var id = await _mediator.Send(command);
            return Created($"/api/games/{gameId}/questions/{questionId}/answers/{id}", null);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAnswerAsync(string gameId, string questionId, [FromBody] DeleteAnswersCommand command)
        {
            var ntid = User.GetUserNTID();
            command.GameId = gameId;
            command.QuestionId = questionId;
            command.UserNTID = ntid;
            var id = await _mediator.Send(command);
            return Created($"/api/games/{gameId}/questions/{questionId}/answers/{id}", null);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateAnswerAsync(string gameId, string questionId, [FromBody] UpdateAnswerCommand command)
        {
            var ntid = User.GetUserNTID();
            command.UserNTID = ntid;
            command.QuestionId = questionId;
            command.GameId = gameId;
            var id = await _mediator.Send(command);
            return Created($"/api/games/{gameId}/questions/{questionId}/answers/{id}", null);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnswerByIdAsync(string gameId, string questionId, string id)
        {
            var ntid = User.GetUserNTID();
            var command = new DeleteAnswerByIdCommand(gameId, questionId, id, ntid);
            await _mediator.Send(command);
            return NoContent();
        }
    }
}