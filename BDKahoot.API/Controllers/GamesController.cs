using BDKahoot.Application.Extensions;
using BDKahoot.Application.Games.Commands.CreateGame;
using BDKahoot.Application.Games.Commands.DeleteGame;
using BDKahoot.Application.Games.Commands.DeleteGameBackground;
using BDKahoot.Application.Games.Commands.UpdateGame;
using BDKahoot.Application.Games.Commands.UpdateGameState;
using BDKahoot.Application.Games.Commands.UploadGameBackground;
using BDKahoot.Application.Games.Dtos;
using BDKahoot.Application.Games.Queries.GetAllGames;
using BDKahoot.Application.Games.Queries.GetGameBackground;
using BDKahoot.Application.Games.Queries.GetGameById;
using BDKahoot.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BDKahoot.API.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IMediator _mediator;

    public GamesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetGamesAsync(
        [FromQuery] int? skip = null, 
        [FromQuery] int? take = null, 
        [FromQuery] string? search = null, 
        [FromQuery] GameState? state = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] SortDirection sortDirection = SortDirection.Descending
        )
    {
        var ntid = User.GetUserNTID();
        //var ntid = "dto6hc@bosch.com"; // For testing purposes, replace with actual user NTID retrieval logic
        var query = new GetAllGamesQuery
        {
            UserNTID = ntid,
            Skip = skip,
            Take = take,
            SearchTerm = search,
            StateFilter = state,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        // Send query
        var games = await _mediator.Send(query);
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGameByIdAsync([FromRoute] string id)
    {
        var ntid = User.GetUserNTID();
        //var ntid = "dto6hc@bosch.com"; // For testing purposes, replace with actual user NTID retrieval logic
        var query = new GetGameByIdQuery
        {
            Id = id,
            UserNTID = ntid
        };

        // Send query
        var game = await _mediator.Send(query);
        return Ok(game);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGameAsync([FromBody] CreateGameCommand command)
    {
        var ntid = User.GetUserNTID();
        command.UserNTID = ntid;

        // Send command
        var id = await _mediator.Send(command);
        return Created($"/api/games/{id}", null);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateGameAsync([FromRoute] string id, UpdateGameCommand command)
    {
        var ntid = User.GetUserNTID();
        command.UserNTID = ntid;
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGameAsync([FromRoute] string id)
    {
        var ntid = User.GetUserNTID();
        //var ntid = "dto6hc@bosch.com"; // For testing purposes, replace with actual user NTID retrieval logic
        var command = new DeleteGameCommand
        {
            Id = id,
            UserNTID = ntid
        };

        // Send command
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("{id}/state")]
    public async Task<IActionResult> UpdateGameStateAsync([FromRoute] string id, UpdateGameStateCommand command)
    {
        var ntid = User.GetUserNTID();
        //var ntid = "dto6hc@bosch.com"; // For testing purposes, replace with actual user NTID retrieval logic
        command.Id = id;
        command.UserNTID = ntid;

        // Send command
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{id}/background")]
    public async Task<IActionResult> GetGameBackgroundAsync([FromRoute] string id)
    {
        var ntid = User.GetUserNTID();
        var query = new GetGameBackgroundQuery
        {
            Id = id,
            UserNTID = ntid
        };

        // Send query
        var memoryStream = await _mediator.Send(query);
        string base64 = Convert.ToBase64String(memoryStream.ToArray());
        return Ok(base64);
    }

    [HttpPost("{id}/background")]
    public async Task<IActionResult> UploadGameBackgroundAsync([FromRoute] string id, [FromForm] UploadGameBackgroundCommand command)
    {
        var ntid = User.GetUserNTID();
        command.Id = id;
        command.UserNTID = ntid;

        // Send command
        await _mediator.Send(command);
        return Ok();
    }

    [HttpDelete("{id}/background")]
    public async Task<IActionResult> DeleteGameBackgroundAsync([FromRoute] string id)
    {
        var ntid = User.GetUserNTID();
        var query = new DeleteGameBackgroundCommand
        {
            Id = id,
            UserNTID = ntid
        };

        // Send command
        await _mediator.Send(query);
        return NoContent();
    }
}