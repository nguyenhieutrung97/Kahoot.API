using BDKahoot.API.Controllers;
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
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace BDKahoot.UnitTests.BDKahoot.API.Controllers
{
    public class GamesControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly GamesController _controller;

        public GamesControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new GamesController(_mediatorMock.Object);
            
            // Setup controller context with user claims for User.GetUserNTID() to work
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Upn, "test@bosch.com")
            }));
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetGamesAsync_ReturnsOkWithGames()
        {
            // Arrange
            var games = new List<GameDto>
            {
                new() { Id = Guid.NewGuid().ToString(), Title = "Game 1", Description = "Game description", CreatedOn = new DateTime(2025, 7, 30)  },
                new() { Id = Guid.NewGuid().ToString(), Title = "Game 2", Description = "Game description 2", CreatedOn = new DateTime(2025, 6, 30) }
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.IsAny<GetAllGamesQuery>(),
                    It.IsAny<CancellationToken>()
                 ))
                .ReturnsAsync(games);

            // Act
            var result = await _controller.GetGamesAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.StatusCode.Should().Be(200); //Assert.Equal(200, okResult.StatusCode);

            var returnedGames = Assert.IsAssignableFrom<IEnumerable<GameDto>>(okResult.Value);
            returnedGames.Should().HaveCount(2); // Assert.Equal(2, returnedGames.Count());
        }

        [Fact]
        public async Task GetGamesAsync_WithNullParameters_SetsDefaultValues()
        {
            // Arrange
            var games = new List<GameDto>
            {
                new() { Id = Guid.NewGuid().ToString(), Title = "Game 1", Description = "Game description", CreatedOn = new DateTime(2025, 7, 30) }
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<GetAllGamesQuery>(q => 
                        q.Skip == null && 
                        q.Take == null && 
                        q.SearchTerm == null && 
                        q.StateFilter == null &&
                        q.SortBy == null &&
                        q.SortDirection == SortDirection.Descending &&
                        q.UserNTID == "test@bosch.com"),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(games);

            // Act
            var result = await _controller.GetGamesAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.StatusCode.Should().Be(200);
            var returnedGames = Assert.IsAssignableFrom<IEnumerable<GameDto>>(okResult.Value);
            returnedGames.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetGamesAsync_WithFilters_ReturnsFilteredGames()
        {
            // Arrange
            var games = new List<GameDto>
            {
                new() { Id = Guid.NewGuid().ToString(), Title = "Filtered Game", Description = "Game description", CreatedOn = new DateTime(2025, 7, 30) }
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<GetAllGamesQuery>(q => 
                        q.Skip == 0 && 
                        q.Take == 10 && 
                        q.SearchTerm == "test" && 
                        q.StateFilter == GameState.Draft &&
                        q.SortBy == "title" &&
                        q.SortDirection == SortDirection.Ascending),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(games);

            // Act
            var result = await _controller.GetGamesAsync(
                skip: 0, 
                take: 10, 
                search: "test", 
                state: GameState.Draft,
                sortBy: "title",
                sortDirection: SortDirection.Ascending);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.StatusCode.Should().Be(200);
            var returnedGames = Assert.IsAssignableFrom<IEnumerable<GameDto>>(okResult.Value);
            returnedGames.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetGamesAsync_CallsMediatrWithCorrectUserNTID()
        {
            // Arrange
            var games = new List<GameDto>();
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllGamesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(games);

            // Act
            await _controller.GetGamesAsync();

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetAllGamesQuery>(q => q.UserNTID == "test@bosch.com"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetGameByIdAsync_ReturnsOkWithGame()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var gameDto = new GameDto() { Id = id, Title = "Game 1", Description = "Game description", CreatedOn = new DateTime(2025, 7, 30) };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<GetGameByIdQuery>(q => q.Id == id && q.UserNTID == "test@bosch.com"),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(gameDto);

            // Act
            var result = await _controller.GetGameByIdAsync(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var returnedGame = Assert.IsType<GameDto>(okResult.Value);
            Assert.Equal(id, returnedGame.Id);
        }

        [Fact]
        public async Task CreateGameAsync_ReturnsCreatedWithLocation()
        {
            // Arrange
            var command = new CreateGameCommand();
            var newId = "new-game-id";
            _mediatorMock
                .Setup(m => m.Send(It.Is<CreateGameCommand>(c => c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newId);

            // Act
            var result = await _controller.CreateGameAsync(command);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            createdResult.Location.Should().Be($"/api/games/{newId}");
        }

        [Fact]
        public async Task UpdateGameAsync_ReturnsNoContent()
        {
            // Arrange
            var id = "update-game-id";
            var command = new UpdateGameCommand();
            _mediatorMock
                .Setup(m => m.Send(It.Is<UpdateGameCommand>(c => c.Id == id && c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.UpdateGameAsync(id, command);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteGameAsync_ReturnsNoContent()
        {
            // Arrange
            var id = "delete-game-id";
            _mediatorMock
                .Setup(m => m.Send(It.Is<DeleteGameCommand>(c => c.Id == id && c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.DeleteGameAsync(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task UpdateGameStateAsync_ReturnsNoContent()
        {
            // Arrange
            var id = "game-id";
            var command = new UpdateGameStateCommand();
            _mediatorMock
                .Setup(m => m.Send(It.Is<UpdateGameStateCommand>(c => c.Id == id && c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.UpdateGameStateAsync(id, command);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mediatorMock.Verify(m => m.Send(It.Is<UpdateGameStateCommand>(c => c.Id == id && c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadBackground_ReturnsOkWithMessage()
        {
            // Arrange
            var id = "game-id";
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024);
            
            var command = new UploadGameBackgroundCommand
            {
                File = mockFile.Object
            };
            _mediatorMock
                .Setup(m => m.Send(It.Is<UploadGameBackgroundCommand>(c => c.Id == id && c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.UploadGameBackgroundAsync(id, command);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task GetGameBackground_ReturnsOkWithBase64String()
        {
            // Arrange
            var id = "game-id";
            var testData = "test image data"u8.ToArray();
            var stream = new MemoryStream(testData);
            var expectedBase64 = Convert.ToBase64String(testData);

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetGameBackgroundQuery>(q => q.Id == id && q.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stream);

            // Act
            var result = await _controller.GetGameBackgroundAsync(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.Value.Should().Be(expectedBase64);
            _mediatorMock.Verify(m => m.Send(It.Is<GetGameBackgroundQuery>(q => q.Id == id && q.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetGameBackground_WithEmptyStream_ReturnsEmptyBase64()
        {
            // Arrange
            var id = "game-id";
            var emptyStream = new MemoryStream();
            var expectedBase64 = Convert.ToBase64String(Array.Empty<byte>());

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetGameBackgroundQuery>(q => q.Id == id && q.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyStream);

            // Act
            var result = await _controller.GetGameBackgroundAsync(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.Value.Should().Be(expectedBase64);
        }

        [Fact]
        public async Task DeleteGameBackground_ReturnsOkWithMessage()
        {
            // Arrange
            var id = "game-id";
            _mediatorMock
                .Setup(m => m.Send(It.Is<DeleteGameBackgroundCommand>(c => c.Id == id && c.UserNTID == "test@bosch.com"), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.DeleteGameBackgroundAsync(id);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
        }

        [Theory]
        [InlineData("Game1")]
        [InlineData("Game2")]
        [InlineData("Game3")]
        public async Task CreateGameAsync_WithDifferentCommands_ReturnsCreatedWithCorrectLocation(string gameTitle)
        {
            // Arrange
            var command = new CreateGameCommand { Title = gameTitle };
            var newId = $"game-{gameTitle.ToLower()}";
            _mediatorMock
                .Setup(m => m.Send(It.Is<CreateGameCommand>(c => c.UserNTID == "test@bosch.com" && c.Title == gameTitle), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newId);

            // Act
            var result = await _controller.CreateGameAsync(command);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            createdResult.Location.Should().Be($"/api/games/{newId}");
        }
    }
}