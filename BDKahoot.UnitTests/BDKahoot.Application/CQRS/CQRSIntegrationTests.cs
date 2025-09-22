using BDKahoot.Application.Games.Commands.CreateGame;
using BDKahoot.Application.Games.Commands.DeleteGame;
using BDKahoot.Application.Games.Queries.GetGameById;
using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Application.Questions.Commands.DeleteQuestion;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Application.Questions.Queries.GetAllQuestions;
using BDKahoot.Application.Questions.Queries.GetQuestionById;
using BDKahoot.Application.Games.Dtos;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Exceptions;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;

namespace BDKahoot.UnitTests.BDKahoot.Application.CQRS
{
    public class CQRSIntegrationTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IQuestionRepository> _questionRepositoryMock;
        private readonly Mock<IAnswerRepository> _answerRepositoryMock;
        private readonly IMediator _mediator;

        public CQRSIntegrationTests()
        {
            // Setup mocks
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _questionRepositoryMock = new Mock<IQuestionRepository>();
            _answerRepositoryMock = new Mock<IAnswerRepository>();

            _unitOfWorkMock.Setup(x => x.Games).Returns(_gameRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Questions).Returns(_questionRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.Answers).Returns(_answerRepositoryMock.Object);

            // Setup DI container
            var services = new ServiceCollection();
            
            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateGameCommand).Assembly));
            
            // Register mocks
            services.AddSingleton(_unitOfWorkMock.Object);
            
            // Register loggers
            services.AddLogging(builder => builder.AddConsole());
            
            // Register AutoMapper
            services.AddAutoMapper(typeof(CreateGameCommand).Assembly);

            _serviceProvider = services.BuildServiceProvider();
            _mediator = _serviceProvider.GetRequiredService<IMediator>();
        }

        #region Game CQRS Integration Tests

        [Fact]
        public async Task GameCQRS_CreateAndRetrieveGame_ShouldWorkEndToEnd()
        {
            // Arrange
            var userNTID = "test@bosch.com";
            var gameTitle = "Integration Test Game";
            var gameId = "created-game-id";

            var createCommand = new CreateGameCommand
            {
                Title = gameTitle,
                UserNTID = userNTID
            };

            var createdGame = new Game
            {
                Id = gameId,
                Title = gameTitle,
                HostUserNTID = userNTID,
                State = GameState.Draft
            };

            var gameDto = new GameDto
            {
                Id = gameId,
                Title = gameTitle,
                HostUserNTID = userNTID,
                State = GameState.Draft
            };

            // Setup mocks for create
            var mapper = _serviceProvider.GetRequiredService<IMapper>();
            _gameRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Game>()))
                .ReturnsAsync(gameId);

            // Setup mocks for retrieve
            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(createdGame);

            // Act - Create game
            var createResult = await _mediator.Send(createCommand);

            // Act - Retrieve game
            var getQuery = new GetGameByIdQuery();
            getQuery.Id = gameId;
            getQuery.UserNTID = userNTID;
            var retrievedGame = await _mediator.Send(getQuery);

            // Assert
            createResult.Should().Be(gameId);
            retrievedGame.Should().NotBeNull();
            retrievedGame.Id.Should().Be(gameId);
            retrievedGame.Title.Should().Be(gameTitle);
            retrievedGame.HostUserNTID.Should().Be(userNTID);
        }

        [Fact]
        public async Task GameCQRS_CreateAndDeleteGame_ShouldWorkEndToEnd()
        {
            // Arrange
            var userNTID = "test@bosch.com";
            var gameId = "test-game-id";

            var game = new Game
            {
                Id = gameId,
                Title = "Game to Delete",
                HostUserNTID = userNTID,
                Deleted = false
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(new List<Question>());

            // Act
            var deleteCommand = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            await _mediator.Send(deleteCommand);

            // Assert
            game.Deleted.Should().BeTrue();
            game.DeletedOn.Should().NotBeNull();
            _gameRepositoryMock.Verify(x => x.UpdateAsync(game), Times.Once);
        }

        #endregion

        #region Question CQRS Integration Tests

        [Fact]
        public async Task QuestionCQRS_CreateRetrieveUpdateDelete_ShouldWorkEndToEnd()
        {
            // Arrange
            var userNTID = "test@bosch.com";
            var gameId = "test-game-id";
            var questionId = "test-question-id";

            var game = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = userNTID
            };

            var question = new Question
            {
                Id = questionId,
                GameId = gameId,
                Title = "Original Question",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice,
                Deleted = false
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _questionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Question>()))
                .ReturnsAsync(questionId);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(questionId))
                .ReturnsAsync(question);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(new List<Question> { question });
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID(questionId))
                .ReturnsAsync(new List<Answer>());

            // Act & Assert - Create Question
            var createCommand = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = userNTID,
                Title = "Original Question",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            var createdQuestionId = await _mediator.Send(createCommand);
            createdQuestionId.Should().Be(questionId);

            // Act & Assert - Get Question by ID
            var getByIdQuery = new GetQuestionByIdQuery(gameId, questionId, userNTID);
            var retrievedQuestion = await _mediator.Send(getByIdQuery);
            retrievedQuestion.Should().NotBeNull();
            retrievedQuestion.Id.Should().Be(questionId);

            // Act & Assert - Get All Questions
            var getAllQuery = new GetAllQuestionQuery(gameId, userNTID);
            var allQuestions = await _mediator.Send(getAllQuery);
            allQuestions.Should().HaveCount(1);
            allQuestions.First().Id.Should().Be(questionId);

            // Act & Assert - Update Question
            var updateCommand = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = userNTID,
                Title = "Updated Question",
                TimeLimitSeconds = 45
            };

            await _mediator.Send(updateCommand);
            question.UpdatedOn.Should().NotBeNull();

            // Act & Assert - Delete Question
            var deleteCommand = new DeleteQuestionCommand(gameId, questionId, userNTID);
            await _mediator.Send(deleteCommand);
            question.Deleted.Should().BeTrue();
            question.DeletedOn.Should().NotBeNull();
        }

        [Fact]
        public async Task QuestionCQRS_UnauthorizedAccess_ShouldThrowException()
        {
            // Arrange
            var gameOwner = "owner@bosch.com";
            var unauthorizedUser = "unauthorized@bosch.com";
            var gameId = "test-game-id";
            var questionId = "test-question-id";

            var game = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = gameOwner
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(game);

            // Act & Assert - Create Question (Unauthorized)
            var createCommand = new CreateQuestionCommand
            {
                GameId = gameId,
                UserNTID = unauthorizedUser,
                Title = "Unauthorized Question",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };

            await Assert.ThrowsAsync<UnauthorizedAccessExceptionCustom>(
                () => _mediator.Send(createCommand));

            // Act & Assert - Get Question (Unauthorized)
            var getQuery = new GetQuestionByIdQuery(gameId, questionId, unauthorizedUser);
            await Assert.ThrowsAsync<UnauthorizedAccessExceptionCustom>(
                () => _mediator.Send(getQuery));

            // Act & Assert - Update Question (Unauthorized)
            var updateCommand = new UpdateQuestionCommand
            {
                GameId = gameId,
                QuestionId = questionId,
                UserNTID = unauthorizedUser,
                Title = "Updated Title",
                TimeLimitSeconds = 45
            };

            await Assert.ThrowsAsync<UnauthorizedAccessExceptionCustom>(
                () => _mediator.Send(updateCommand));

            // Act & Assert - Delete Question (Unauthorized)
            var deleteCommand = new DeleteQuestionCommand(gameId, questionId, unauthorizedUser);
            await Assert.ThrowsAsync<UnauthorizedAccessExceptionCustom>(
                () => _mediator.Send(deleteCommand));
        }

        #endregion

        #region Cross-Entity CQRS Integration Tests

        [Fact]
        public async Task CrossEntityCQRS_GameWithQuestions_DeleteCascade_ShouldWorkCorrectly()
        {
            // Arrange
            var userNTID = "test@bosch.com";
            var gameId = "test-game-id";

            var game = new Game
            {
                Id = gameId,
                Title = "Game with Questions",
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
                new() { Id = "a1", QuestionId = "q1", Title = "Answer 1", Deleted = false }
            };

            var answersForQ2 = new List<Answer>
            {
                new() { Id = "a2", QuestionId = "q2", Title = "Answer 2", Deleted = false }
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _questionRepositoryMock.Setup(x => x.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q1"))
                .ReturnsAsync(answersForQ1);
            _answerRepositoryMock.Setup(x => x.GetAnswerByQuestionID("q2"))
                .ReturnsAsync(answersForQ2);

            // Act
            var deleteGameCommand = new DeleteGameCommand
            {
                Id = gameId,
                UserNTID = userNTID
            };

            await _mediator.Send(deleteGameCommand);

            // Assert
            game.Deleted.Should().BeTrue();
            
            foreach (var question in questions)
            {
                question.Deleted.Should().BeTrue();
            }

            foreach (var answer in answersForQ1.Concat(answersForQ2))
            {
                answer.Deleted.Should().BeTrue();
            }

            _gameRepositoryMock.Verify(x => x.UpdateAsync(game), Times.Once);
            _questionRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Question>()), Times.Exactly(2));
            _answerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Answer>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CrossEntityCQRS_MultipleQuestionTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var userNTID = "test@bosch.com";
            var gameId = "test-game-id";

            var game = new Game
            {
                Id = gameId,
                Title = "Multi-Type Question Game",
                HostUserNTID = userNTID
            };

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(gameId))
                .ReturnsAsync(game);

            var questionTypes = new[]
            {
                QuestionType.SingleChoice,
                QuestionType.MultipleChoice,
                QuestionType.TrueFalse
            };

            var createdQuestionIds = new List<string>();

            // Act - Create questions of different types
            foreach (var (questionType, index) in questionTypes.Select((type, i) => (type, i)))
            {
                var questionId = $"question-{index}";
                _questionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Question>()))
                    .ReturnsAsync(questionId);

                var command = new CreateQuestionCommand
                {
                    GameId = gameId,
                    UserNTID = userNTID,
                    Title = $"Question {index + 1} - {questionType}",
                    TimeLimitSeconds = 30 + (index * 15),
                    Type = questionType
                };

                var result = await _mediator.Send(command);
                createdQuestionIds.Add(result);
            }

            // Assert
            createdQuestionIds.Should().HaveCount(3);
            _questionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Question>()), Times.Exactly(3));
        }

        #endregion

        #region Error Handling Integration Tests

        [Fact]
        public async Task CQRS_NonExistentEntities_ShouldThrowNotFoundException()
        {
            // Arrange
            var userNTID = "test@bosch.com";
            var nonExistentGameId = "non-existent-game";
            var nonExistentQuestionId = "non-existent-question";

            _gameRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentGameId))
                .ReturnsAsync((Game?)null);
            _questionRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentQuestionId))
                .ReturnsAsync((Question?)null);

            // Act & Assert - Game not found
            var getGameQuery = new GetGameByIdQuery();
            getGameQuery.Id = nonExistentGameId;
            getGameQuery.UserNTID = userNTID;
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _mediator.Send(getGameQuery));

            var deleteGameCommand = new DeleteGameCommand
            {
                Id = nonExistentGameId,
                UserNTID = userNTID
            };
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _mediator.Send(deleteGameCommand));

            // Act & Assert - Question operations on non-existent game
            var createQuestionCommand = new CreateQuestionCommand
            {
                GameId = nonExistentGameId,
                UserNTID = userNTID,
                Title = "Question for non-existent game",
                TimeLimitSeconds = 30,
                Type = QuestionType.SingleChoice
            };
            await Assert.ThrowsAsync<NotFoundExceptionCustom>(
                () => _mediator.Send(createQuestionCommand));
        }

        #endregion

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
