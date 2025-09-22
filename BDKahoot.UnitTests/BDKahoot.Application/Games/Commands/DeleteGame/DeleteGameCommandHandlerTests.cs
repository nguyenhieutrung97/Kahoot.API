using BDKahoot.Application.Games.Commands.DeleteGame;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Games.Commands.DeleteGame
{
    public class DeleteGameCommandHandlerTests
    {
        private readonly Mock<ILogger<DeleteGameCommandHandler>> _loggerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly Mock<IAnswerRepository> _answerRepositoryMock;
        private readonly DeleteGameCommandHandler _handler;

        public DeleteGameCommandHandlerTests()
        {
            _loggerMock = new Mock<ILogger<DeleteGameCommandHandler>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();
            _answerRepositoryMock = new Mock<IAnswerRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Answers).Returns(_answerRepositoryMock.Object);

            _handler = new DeleteGameCommandHandler(
                _loggerMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldSoftDeleteGameQuestionsAndAnswers()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID,
                Deleted = false
            };

            var questions = new List<Question>
            {
                new() { Id = "q1", GameId = gameId, Title = "Question 1", Deleted = false },
                new() { Id = "q2", GameId = gameId, Title = "Question 2", Deleted = false }
            };

            var answersForQ1 = new List<Answer>
            {
                new() { Id = "a1", QuestionId = "q1", Title = "Answer 1", Deleted = false },
                new() { Id = "a2", QuestionId = "q1", Title = "Answer 2", Deleted = false }
            };

            var answersForQ2 = new List<Answer>
            {
                new() { Id = "a3", QuestionId = "q2", Title = "Answer 3", Deleted = false }
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q1"))
                .ReturnsAsync(answersForQ1);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q2"))
                .ReturnsAsync(answersForQ2);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            // Verify game is soft deleted
            existingGame.Deleted.Should().BeTrue();
            existingGame.DeletedOn.Should().NotBeNull();
            existingGame.DeletedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            // Verify questions are soft deleted
            foreach (var question in questions)
            {
                question.Deleted.Should().BeTrue();
                question.DeletedOn.Should().NotBeNull();
                question.DeletedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            }

            // Verify answers are soft deleted
            foreach (var answer in answersForQ1.Concat(answersForQ2))
            {
                answer.Deleted.Should().BeTrue();
                answer.DeletedOn.Should().NotBeNull();
                answer.DeletedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            }

            // Verify repository calls
            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _gameRepositoryMock.Verify(x => x.UpdateAsync(existingGame), Times.Once);
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(gameId), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Exactly(questions.Count));
            _answerRepositoryMock.Verify(x => x.GetAnswerByQuestionID("q1"), Times.Once);
            _answerRepositoryMock.Verify(x => x.GetAnswerByQuestionID("q2"), Times.Once);
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Exactly(3));
        }

        [Fact]
        public async Task Handle_GameNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var gameId = "non-existent-game-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync((Game?)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _gameRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Game>()), Times.Never);
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnauthorizedUser_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var gameId = "test-game-id";
            var gameOwner = "owner@bosch.com";
            var unauthorizedUser = "unauthorized@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = unauthorizedUser
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = gameOwner,
                Deleted = false
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessExceptionCustom>(
                () => _handler.Handle(command, CancellationToken.None));

            _gameRepositoryMock.Verify(x => x.GetByIdAsync(gameId), Times.Once);
            _gameRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Game>()), Times.Never);
            _questionRepositoryMock.Verify(x => x.GetQuestionsByGameIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_GameWithNoQuestions_ShouldDeleteGameOnly()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID,
                Deleted = false
            };

            var emptyQuestions = new List<Question>();

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(emptyQuestions);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingGame.Deleted.Should().BeTrue();
            existingGame.DeletedOn.Should().NotBeNull();

            _gameRepositoryMock.Verify(x => x.UpdateAsync(existingGame), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Never);
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Never);
        }

        [Fact]
        public async Task Handle_QuestionsWithNoAnswers_ShouldDeleteGameAndQuestions()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID,
                Deleted = false
            };

            var questions = new List<Question>
            {
                new() { Id = "q1", GameId = gameId, Title = "Question 1", Deleted = false }
            };

            var emptyAnswers = new List<Answer>();

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q1"))
                .ReturnsAsync(emptyAnswers);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingGame.Deleted.Should().BeTrue();
            questions.First().Deleted.Should().BeTrue();

            _gameRepositoryMock.Verify(x => x.UpdateAsync(existingGame), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Once);
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldLogCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID,
                Deleted = false
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(new List<Question>());

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Deleting game with id: {gameId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_MultipleQuestionsWithMixedAnswers_ShouldDeleteAllCorrectly()
        {
            // Arrange
            var gameId = "test-game-id";
            var userNTID = "test@bosch.com";

            var command = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            var existingGame = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID,
                Deleted = false
            };

            var questions = new List<Question>
            {
                new() { Id = "q1", GameId = gameId, Title = "Question 1", Deleted = false },
                new() { Id = "q2", GameId = gameId, Title = "Question 2", Deleted = false },
                new() { Id = "q3", GameId = gameId, Title = "Question 3", Deleted = false }
            };

            // Q1 has 2 answers, Q2 has 0 answers, Q3 has 1 answer
            var answersForQ1 = new List<Answer>
            {
                new() { Id = "a1", QuestionId = "q1", Deleted = false },
                new() { Id = "a2", QuestionId = "q1", Deleted = false }
            };
            var answersForQ2 = new List<Answer>(); // Empty
            var answersForQ3 = new List<Answer>
            {
                new() { Id = "a3", QuestionId = "q3", Deleted = false }
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(existingGame);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q1"))
                .ReturnsAsync(answersForQ1);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q2"))
                .ReturnsAsync(answersForQ2);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q3"))
                .ReturnsAsync(answersForQ3);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            existingGame.Deleted.Should().BeTrue();

            foreach (var question in questions)
            {
                question.Deleted.Should().BeTrue();
            }

            foreach (var answer in answersForQ1.Concat(answersForQ3))
            {
                answer.Deleted.Should().BeTrue();
            }

            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Exactly(3));
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Exactly(3)); // 2 from Q1 + 1 from Q3
        }
    }
}
