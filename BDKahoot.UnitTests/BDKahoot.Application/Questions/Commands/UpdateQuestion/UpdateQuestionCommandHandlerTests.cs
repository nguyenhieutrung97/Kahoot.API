using AutoMapper;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Application.Games.Commands.UpdateGame;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Questions.Commands.UpdateQuestion
{
    public class UpdateQuestionCommandHandlerTests
    {
        private readonly Mock<ILogger<UpdateGameCommandHandler>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly UpdateQuestionCommandHandler _handler;

        public UpdateQuestionCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<UpdateGameCommandHandler>>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);

            _handler = new UpdateQuestionCommandHandler(
                _loggerMock.Object,
                _mapperMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldUpdateQuestionSuccessfully()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var existingQuestion = new Question
            {
                Id = questionId,
                GameId = gameId,
                Title = "Original Question Title",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice,
                UpdatedOn = null
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingQuestion.UpdatedOn.Should().NotBeNull();
            existingQuestion.UpdatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
            _mapperMock.Verify(x => x.Map(command, existingQuestion), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(existingQuestion), Times.Once);
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "non-existent-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync((Game?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var gameOwner = "owner@bosch.com";
            var unauthorizedUser = "unauthorized@bosch.com";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = unauthorizedUser,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = gameOwner
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Never);
        }

        [Fact]
        public async Task Handle_QuestionNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "non-existent-question-id";
            var userNTID = "test@bosch.com";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync((Question?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Never);
        }

        [Fact]
        public async Task Handle_QuestionBelongsToDifferentGame_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";
            var differentGameId = "different-game-id";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var existingQuestion = new Question
            {
                Id = questionId,
                GameId = differentGameId, // Question belongs to different game
                Title = "Original Question Title",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
            _mapperMock.Verify(x => x.Map(It.IsAny<UpdateQuestionCommand>(), It.IsAny<Question>()), Times.Never);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Never);
        }

        [Theory]
        [InlineData("Updated Title 1", 15)]
        [InlineData("Updated Title 2", 60)]
        [InlineData("Updated Title 3", 120)]
        public async Task Handle_DifferentUpdateValues_ShouldUpdateCorrectly(string newTitle, int newTimeLimitSeconds)
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = newTitle,
                TimeLimitSeconds = newTimeLimitSeconds
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var existingQuestion = new Question
            {
                Id = questionId,
                GameId = gameId,
                Title = "Original Title",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice,
                UpdatedOn = null
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingQuestion.UpdatedOn.Should().NotBeNull();
            _mapperMock.Verify(x => x.Map(command, existingQuestion), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(existingQuestion), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldLogCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var existingQuestion = new Question
            {
                Id = questionId,
                GameId = gameId,
                Title = "Original Question Title",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Updating question with id: {questionId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldPreserveQuestionType()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";
            var originalType = QuestionType.MultipleChoice;

            var command = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question Title",
                TimeLimitSeconds = 45
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var existingQuestion = new Question
            {
                Id = questionId,
                GameId = gameId,
                Title = "Original Question Title",
                TimeLimitSeconds = 30,
                Type = originalType // This should be preserved
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingQuestion.Type.Should().Be(originalType); // Type should remain unchanged
            _mapperMock.Verify(x => x.Map(command, existingQuestion), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(existingQuestion), Times.Once);
        }
    }
}
