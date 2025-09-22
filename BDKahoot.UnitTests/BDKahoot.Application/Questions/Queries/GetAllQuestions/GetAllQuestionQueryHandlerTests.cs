using AutoMapper;
using BDKahoot.Application.Questions.Queries.GetAllQuestions;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Questions.Queries.GetAllQuestions
{
    public class GetAllQuestionQueryHandlerTests
    {
        private readonly Mock<ILogger<GetAllQuestionQueryHandler>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly GetAllQuestionQueryHandler _handler;

        public GetAllQuestionQueryHandlerTests()
        {
            _loggerMock = new Mock<ILogger<GetAllQuestionQueryHandler>>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);

            _handler = new GetAllQuestionQueryHandler(
                _loggerMock.Object,
                _mapperMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnAllQuestions()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var query = new GetAllQuestionQuery(gameId, userNTID);

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var questions = new List<Question>
            {
                new() { Id = "q1", GameId = gameId, Title = "Question 1", Type = QuestionType.SingleChoice, TimeLimitSeconds = 30 },
                new() { Id = "q2", GameId = gameId, Title = "Question 2", Type = QuestionType.MultipleChoice, TimeLimitSeconds = 45 },
                new() { Id = "q3", GameId = gameId, Title = "Question 3", Type = QuestionType.TrueFalse, TimeLimitSeconds = 20 }
            };

            var questionDtos = new List<QuestionDto>
            {
                new() { Id = "q1", GameId = gameId, Title = "Question 1", Type = QuestionType.SingleChoice, TimeLimitSeconds = 30 },
                new() { Id = "q2", GameId = gameId, Title = "Question 2", Type = QuestionType.MultipleChoice, TimeLimitSeconds = 45 },
                new() { Id = "q3", GameId = gameId, Title = "Question 3", Type = QuestionType.TrueFalse, TimeLimitSeconds = 20 }
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _mapperMock.Setup(x => x.Map<IEnumerable<QuestionDto>>(questions))
                .Returns(questionDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(questionDtos);

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(gameId), Times.Once);
            _mapperMock.Verify(x => x.Map<IEnumerable<QuestionDto>>(questions), Times.Once);
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "non-existent-game-id";
            var userNTID = "test@bosch.com";

            var query = new GetAllQuestionQuery(gameId, userNTID);

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync((Game?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(query, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var gameId = "test-game-id";
            var gameOwner = "owner@bosch.com";
            var unauthorizedUser = "unauthorized@bosch.com";

            var query = new GetAllQuestionQuery(gameId, unauthorizedUser);

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
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NoQuestionsFound_ShouldReturnEmptyCollection()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var query = new GetAllQuestionQuery(gameId, userNTID);

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var emptyQuestions = new List<Question>();
            var emptyQuestionDtos = new List<QuestionDto>();

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(emptyQuestions);
            _mapperMock.Setup(x => x.Map<IEnumerable<QuestionDto>>(emptyQuestions))
                .Returns(emptyQuestionDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(gameId), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldLogCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var query = new GetAllQuestionQuery(gameId, userNTID);

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(new List<Question>());
            _mapperMock.Setup(x => x.Map<IEnumerable<QuestionDto>>(It.IsAny<IEnumerable<Question>>()))
                .Returns(new List<QuestionDto>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Getting all questions for game with Id: {gameId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_MixedQuestionTypes_ShouldReturnAllTypes()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var query = new GetAllQuestionQuery(gameId, userNTID);

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var questions = new List<Question>
            {
                new() { Id = "q1", GameId = gameId, Title = "Single Choice Q", Type = QuestionType.SingleChoice, TimeLimitSeconds = 30 },
                new() { Id = "q2", GameId = gameId, Title = "Multiple Choice Q", Type = QuestionType.MultipleChoice, TimeLimitSeconds = 45 },
                new() { Id = "q3", GameId = gameId, Title = "True/False Q", Type = QuestionType.TrueFalse, TimeLimitSeconds = 20 }
            };

            var questionDtos = questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                GameId = q.GameId,
                Title = q.Title,
                Type = q.Type,
                TimeLimitSeconds = q.TimeLimitSeconds
            }).ToList();

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _mapperMock.Setup(x => x.Map<IEnumerable<QuestionDto>>(questions))
                .Returns(questionDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(q => q.Type == QuestionType.SingleChoice);
            result.Should().Contain(q => q.Type == QuestionType.MultipleChoice);
            result.Should().Contain(q => q.Type == QuestionType.TrueFalse);
        }
    }
}
