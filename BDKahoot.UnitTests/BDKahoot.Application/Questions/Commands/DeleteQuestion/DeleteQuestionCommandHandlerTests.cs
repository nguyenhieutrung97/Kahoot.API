using BDKahoot.Application.Questions.Commands.DeleteQuestion;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Questions.Commands.DeleteQuestion
{
    public class DeleteQuestionCommandHandlerTests
    {
        private readonly Mock<ILogger<DeleteQuestionCommandHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly Mock<IAnswerRepository> _answerRepositoryMock;
        private readonly DeleteQuestionCommandHandler _handler;

        public DeleteQuestionCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<DeleteQuestionCommandHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();
            _answerRepositoryMock = new Mock<IAnswerRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Answers).Returns(_answerRepositoryMock.Object);

            _handler = new DeleteQuestionCommandHandler(
                _loggerMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldSoftDeleteQuestionAndAnswers()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteQuestionCommand(gameId, questionId, userNTID);

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
                Deleted = false
            };

            var answers = new List<Answer>
            {
                new() { Id = "answer1", QuestionId = questionId, Deleted = false },
                new() { Id = "answer2", QuestionId = questionId, Deleted = false }
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID(questionId))
                .ReturnsAsync(answers);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingQuestion.Deleted.Should().BeTrue();
            existingQuestion.DeletedOn.Should().NotBeNull();
            existingQuestion.DeletedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            foreach (var answer in answers)
            {
                answer.Deleted.Should().BeTrue();
                answer.DeletedOn.Should().NotBeNull();
                answer.DeletedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            }

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetByIdAsync(questionId), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(existingQuestion), Times.Once);
            _answerRepositoryMock.Verify(x => x.GetAnswerByQuestionID(questionId), Times.Once);
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Exactly(answers.Count));
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "non-existent-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteQuestionCommand(gameId, questionId, userNTID);

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

            var command = new DeleteQuestionCommand(gameId, questionId, unauthorizedUser);

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

            var command = new DeleteQuestionCommand(gameId, questionId, userNTID);

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

            var command = new DeleteQuestionCommand(gameId, questionId, userNTID);

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
                Deleted = false
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
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Never);
        }

        [Fact]
        public async Task Handle_QuestionWithNoAnswers_ShouldDeleteQuestionOnly()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteQuestionCommand(gameId, questionId, userNTID);

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
                Deleted = false
            };

            var emptyAnswers = new List<Answer>();

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID(questionId))
                .ReturnsAsync(emptyAnswers);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingQuestion.Deleted.Should().BeTrue();
            existingQuestion.DeletedOn.Should().NotBeNull();

            _questionRepositoryMock.Verify(x => x.UpdateAsync(existingQuestion), Times.Once);
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldLogCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var questionId = "test-question-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteQuestionCommand(gameId, questionId, userNTID);

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
                Deleted = false
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(existingQuestion);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID(questionId))
                .ReturnsAsync(new List<Answer>());

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Deleting question with id: {questionId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
