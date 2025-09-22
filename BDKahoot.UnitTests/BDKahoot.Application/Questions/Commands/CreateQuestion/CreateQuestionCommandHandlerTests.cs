using AutoMapper;
using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Questions.Commands.CreateQuestion
{
    public class CreateQuestionCommandHandlerTests
    {
        private readonly Mock<ILogger<CreateQuestionCommandHandler>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly CreateQuestionCommandHandler _handler;

        public CreateQuestionCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<CreateQuestionCommandHandler>>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);

            _handler = new CreateQuestionCommandHandler(
                _loggerMock.Object,
                _mapperMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldCreateQuestionSuccessfully()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";
            var questionId = "new-question-id";

            var command = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = userNTID,
                Title = "Test Question",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var questionToCreate = new Question
            {
                GameId = gameId,
                Title = command.Title,
                TimeLimitSeconds = command.TimeLimitSeconds,
                Type = command.Type
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mapperMock.Setup(x => x.Map<Question>(command))
                .Returns(questionToCreate);
            _questionRepositoryMock.Setup(x => x.AddAsync(questionToCreate))
                .ReturnsAsync(questionId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(questionId);
            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _mapperMock.Verify(x => x.Map<Question>(command), Times.Once);
            _questionRepositoryMock.Verify(x => x.AddAsync(questionToCreate), Times.Once);
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "non-existent-game-id";
            var command = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = "test@bosch.com",
                Title = "Test Question",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync((Game?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Question>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var gameId = "test-game-id";
            var gameOwner = "owner@bosch.com";
            var unauthorizedUser = "unauthorized@bosch.com";

            var command = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = unauthorizedUser,
                Title = "Test Question",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
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
            _questionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Question>()), Times.Never);
        }

        [Theory]
        [InlineData(QuestionType.SingleChoice)]
        [InlineData(QuestionType.MultipleChoice)]
        [InlineData(QuestionType.TrueFalse)]
        public async Task Handle_DifferentQuestionTypes_ShouldCreateSuccessfully(QuestionType questionType)
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";
            var questionId = "new-question-id";

            var command = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = userNTID,
                Title = $"Test {questionType} Question",
                TimeLimitSeconds = 45,
                Type = questionType
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var questionToCreate = new Question
            {
                GameId = gameId,
                Title = command.Title,
                TimeLimitSeconds = command.TimeLimitSeconds,
                Type = command.Type
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mapperMock.Setup(x => x.Map<Question>(command))
                .Returns(questionToCreate);
            _questionRepositoryMock.Setup(x => x.AddAsync(questionToCreate))
                .ReturnsAsync(questionId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(questionId);
            _questionRepositoryMock.Verify(x => x.AddAsync(It.Is<Question>(q => q.Type == questionType)), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldLogCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";
            var questionTitle = "Test Question";

            var command = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = userNTID,
                Title = questionTitle,
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _mapperMock.Setup(x => x.Map<Question>(command))
                .Returns(new Question());
            _questionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Question>()))
                .ReturnsAsync("question-id");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {userNTID} is creating a question: {questionTitle} for the game {gameId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
