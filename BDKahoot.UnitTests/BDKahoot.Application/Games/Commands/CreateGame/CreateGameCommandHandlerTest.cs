using BDKahoot.Application.Games.Commands.CreateGame;
using BDKahoot.Domain.Models;
using BDKahoot.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Games.Commands.CreateGame
{
    public class CreateGameCommandHandlerTest()
    {
        private readonly GameHandlerTestFixture _fixture = new();

        [Fact]
        public async Task CreateGameCommandHandler_ReturnGameId()
        {
            // Arrange
            var handler = _fixture.InitializeCreateHandler();
            var command = new CreateGameCommand
            {
                Title = "Test Game",
                Description = "Test Description",
                UserNTID = "host-id"
            };

            var game = new Game
            {
                Id = "game-123",
                Title = command.Title,
                Description = command.Description
            };

            _fixture.MapperMock.Setup(m => m.Map<Game>(command)).Returns(game);
            _fixture.GameRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Game>())).ReturnsAsync(game.Id);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            // Verify that the returned result has the same Id as the created game
            Assert.Equal(game.Id, result);

            // Verify that an information log was written with the correct message when the user creates a game
            _fixture.GetLoggerMock<CreateGameCommandHandler>()
                .VerifyLogContains(LogLevel.Information, $"User {command.UserNTID} is creating game: {command.Title}");

            // Verify that the mapper was called once to map the command to a Game entity
            _fixture.MapperMock.Verify(m => m.Map<Game>(command), Times.Once);

            // Verify that AddAsync was called once with the correct Game entity
            _fixture.GameRepositoryMock
                .Verify(r => r.AddAsync(It.Is<Game>(g => g == game)), Times.Once);
        }
    }
}
