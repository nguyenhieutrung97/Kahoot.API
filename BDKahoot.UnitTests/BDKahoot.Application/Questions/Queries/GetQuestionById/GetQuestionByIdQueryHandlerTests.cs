using AutoMapper;
using BDKahoot.Application.Questions.Queries.GetQuestionById;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Questions.Queries.GetQuestionById
{
    public class GetQuestionByIdQueryHandlerTests
    {
        private readonly Mock<ILogger<GetQuestionByIdQueryHandler>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly GetQuestionByIdQueryHandler _handler;

        public GetQuestionByIdQueryHandlerTests()
        {
            _loggerMock = new Mock<ILogger<GetQuestionByIdQueryHandler>>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);

            _handler = new GetQuestionByIdQueryHandler(
                _loggerMock.Object,
                _mapperMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnQuestion()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var query = new GetQuestionByIdQuery(gameId, questionId, userNTID);

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
                Title = "Test Question",
                Type = QuestionType.SingleChoice,
                TimeLimitSeconds = 30
            };

            var questionDto = new QuestionDto
            {
                Id = questionId,
                GameId = gameId,
                Title = "Test Question",
                Type = QuestionType.SingleChoice,
                TimeLimitSeconds = 30
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);
            _mapperMock.Setup(x => x.Map<QuestionDto>(existingQuestion))
                .Returns(questionDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(questionDto);

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
            _mapperMock.Verify(x => x.Map<QuestionDto>(existingQuestion), Times.Once);
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "non-existent-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var query = new GetQuestionByIdQuery(gameId, questionId, userNTID);

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync((Game?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(query, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var gameOwner = "owner@bosch.com";
            var unauthorizedUser = "unauthorized@bosch.com";

            var query = new GetQuestionByIdQuery(gameId, questionId, unauthorizedUser);

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
                () => _handler.Handle(query, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_QuestionNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "non-existent-question-id";
            var userNTID = "test@bosch.com";

            var query = new GetQuestionByIdQuery(gameId, questionId, userNTID);

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
                () => _handler.Handle(query, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
        }

        [Fact]
        public async Task Handle_QuestionBelongsToDifferentGame_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";
            var differentGameId = "different-game-id";

            var query = new GetQuestionByIdQuery(gameId, questionId, userNTID);

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
                Title = "Test Question",
                Type = QuestionType.SingleChoice,
                TimeLimitSeconds = 30
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(query, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
            _mapperMock.Verify(x => x.Map<QuestionDto>(It.IsAny<Question>()), Times.Never);
        }

        [Theory]
        [InlineData(QuestionType.SingleChoice)]
        [InlineData(QuestionType.MultipleChoice)]
        [InlineData(QuestionType.TrueFalse)]
        public async Task Handle_DifferentQuestionTypes_ShouldReturnCorrectType(QuestionType questionType)
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var query = new GetQuestionByIdQuery(gameId, questionId, userNTID);

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
                Title = $"Test {questionType} Question",
                Type = questionType,
                TimeLimitSeconds = 30
            };

            var questionDto = new QuestionDto
            {
                Id = questionId,
                GameId = gameId,
                Title = $"Test {questionType} Question",
                Type = questionType,
                TimeLimitSeconds = 30
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);
            _mapperMock.Setup(x => x.Map<QuestionDto>(existingQuestion))
                .Returns(questionDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Type.Should().Be(questionType);
            result.Title.Should().Contain(questionType.ToString());
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldLogCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var query = new GetQuestionByIdQuery(gameId, questionId, userNTID);

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
                Title = "Test Question",
                Type = QuestionType.SingleChoice,
                TimeLimitSeconds = 30
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);
            _mapperMock.Setup(x => x.Map<QuestionDto>(existingQuestion))
                .Returns(new QuestionDto());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Getting question with Id: {questionId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
