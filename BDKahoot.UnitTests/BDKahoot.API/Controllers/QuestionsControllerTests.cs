using BDKahoot.API.Controllers;
using BDKahoot.Application.Questions.Commands.CreateQuestion;
using BDKahoot.Application.Questions.Commands.DeleteQuestion;
using BDKahoot.Application.Questions.Commands.UpdateQuestion;
using BDKahoot.Application.Questions.Dtos;
using BDKahoot.Application.Questions.Queries.GetAllQuestions;
using BDKahoot.Application.Questions.Queries.GetQuestionById;
using BDKahoot.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace BDKahoot.UnitTests.BDKahoot.API.Controllers
{
    public class QuestionsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly QuestionsController _controller;

        public QuestionsControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new QuestionsController(_mediatorMock.Object);
            
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
        public async Task GetQuestionsAsync_ReturnsOkWithQuestions()
        {
            // Arrange
            var gameId = "game-123";
            var questions = new List<QuestionDto>
            {
                new() { Id = "question-1", GameId = gameId, Title = "Question 1", TimeLimitSeconds = 30, Type = QuestionType.MultipleChoice },
                new() { Id = "question-2", GameId = gameId, Title = "Question 2", TimeLimitSeconds = 20, Type = QuestionType.TrueFalse }
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<GetAllQuestionQuery>(q => q.GameId == gameId && q.UserNTID == "test@bosch.com"),
                    It.IsAny<CancellationToken>()
                 ))
                .ReturnsAsync(questions);

            // Act
            var result = await _controller.GetQuestionsAsync(gameId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.StatusCode.Should().Be(200);

            var returnedQuestions = Assert.IsAssignableFrom<IEnumerable<QuestionDto>>(okResult.Value);
            returnedQuestions.Should().HaveCount(2);
            returnedQuestions.First().GameId.Should().Be(gameId);
        }

        [Fact]
        public async Task GetQuestionsAsync_WithEmptyResult_ReturnsOkWithEmptyList()
        {
            // Arrange
            var gameId = "game-123";
            var questions = new List<QuestionDto>();

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<GetAllQuestionQuery>(q => q.GameId == gameId && q.UserNTID == "test@bosch.com"),
                    It.IsAny<CancellationToken>()
                 ))
                .ReturnsAsync(questions);

            // Act
            var result = await _controller.GetQuestionsAsync(gameId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.StatusCode.Should().Be(200);
            var returnedQuestions = Assert.IsAssignableFrom<IEnumerable<QuestionDto>>(okResult.Value);
            returnedQuestions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetQuestionByIdAsync_ReturnsOkWithQuestion()
        {
            // Arrange
            var gameId = "game-123";
            var questionId = "question-456";
            var questionDto = new QuestionDto 
            { 
                Id = questionId, 
                GameId = gameId, 
                Title = "Test Question", 
                TimeLimitSeconds = 30,
                Type = QuestionType.MultipleChoice,
                CreatedOn = DateTime.UtcNow
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<GetQuestionByIdQuery>(q => 
                        q.GameId == gameId && 
                        q.QuestionId == questionId && 
                        q.UserNTID == "test@bosch.com"),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(questionDto);

            // Act
            var result = await _controller.GetQuestionByIdAsync(gameId, questionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            okResult.StatusCode.Should().Be(200);
            var returnedQuestion = Assert.IsType<QuestionDto>(okResult.Value);
            returnedQuestion.Id.Should().Be(questionId);
            returnedQuestion.GameId.Should().Be(gameId);
            returnedQuestion.Type.Should().Be(QuestionType.MultipleChoice);
        }

        [Fact]
        public async Task CreateQuestionAsync_ReturnsCreatedWithLocation()
        {
            // Arrange
            var gameId = "game-123";
            var command = new CreateQuestionCommand
            {
                Title = "New Question",
                TimeLimitSeconds = 30
            };
            var newQuestionId = "new-question-id";

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<CreateQuestionCommand>(c => 
                        c.GameId == gameId && 
                        c.UserNTID == "test@bosch.com" &&
                        c.Title == command.Title &&
                        c.TimeLimitSeconds == command.TimeLimitSeconds), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(newQuestionId);

            // Act
            var result = await _controller.CreateQuestionAsync(gameId, command);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            createdResult.Location.Should().Be($"/api/games/{gameId}/questions/{newQuestionId}");
        }

        [Theory]
        [InlineData("What is 2+2?", 30)]
        [InlineData("What is the capital of France?", 25)]
        [InlineData("Is the sky blue?", 15)]
        public async Task CreateQuestionAsync_WithDifferentData_ReturnsCreated(string title, int timeLimitSeconds)
        {
            // Arrange
            var gameId = "game-123";
            var command = new CreateQuestionCommand
            {
                Title = title,
                TimeLimitSeconds = timeLimitSeconds
            };
            var newQuestionId = $"question-{Guid.NewGuid()}"; // Generate a new ID for each test case

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateQuestionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newQuestionId);

            // Act
            var result = await _controller.CreateQuestionAsync(gameId, command);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result);
            createdResult.Location.Should().Be($"/api/games/{gameId}/questions/{newQuestionId}");
            
            // Verify the command was sent with correct values
            _mediatorMock.Verify(m => m.Send(
                It.Is<CreateQuestionCommand>(c => 
                    c.Title == title && 
                    c.TimeLimitSeconds == timeLimitSeconds &&
                    c.GameId == gameId &&
                    c.UserNTID == "test@bosch.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateQuestionAsync_ReturnsNoContent()
        {
            // Arrange
            var gameId = "game-123";
            var questionId = "question-456";
            var command = new UpdateQuestionCommand
            {
                Title = "Updated Question",
                TimeLimitSeconds = 45
            };

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<UpdateQuestionCommand>(c => 
                        c.GameId == gameId && 
                        c.QuestionId == questionId && 
                        c.UserNTID == "test@bosch.com" &&
                        c.Title == command.Title &&
                        c.TimeLimitSeconds == command.TimeLimitSeconds), 
                    It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.UpdateQuestionAsync(gameId, questionId, command);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mediatorMock.Verify(m => m.Send(
                It.Is<UpdateQuestionCommand>(c => 
                    c.GameId == gameId && 
                    c.QuestionId == questionId && 
                    c.UserNTID == "test@bosch.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateQuestionAsync_SetsCorrectIds()
        {
            // Arrange
            var gameId = "game-123";
            var questionId = "question-456";
            var command = new UpdateQuestionCommand
            {
                Title = "Updated Question",
                TimeLimitSeconds = 15
            };

            // Act
            await _controller.UpdateQuestionAsync(gameId, questionId, command);

            // Assert
            // Verify that the controller correctly sets the IDs from route parameters
            _mediatorMock.Verify(m => m.Send(
                It.Is<UpdateQuestionCommand>(c => 
                    c.GameId == gameId && 
                    c.QuestionId == questionId &&
                    c.UserNTID == "test@bosch.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteQuestionAsync_ReturnsNoContent()
        {
            // Arrange
            var gameId = "game-123";
            var questionId = "question-456";

            _mediatorMock
                .Setup(m => m.Send(
                    It.Is<DeleteQuestionCommand>(c => 
                        c.GameId == gameId && 
                        c.QuestionId == questionId && 
                        c.UserNTID == "test@bosch.com"), 
                    It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.DeleteQuestionAsync(gameId, questionId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mediatorMock.Verify(m => m.Send(
                It.Is<DeleteQuestionCommand>(c => 
                    c.GameId == gameId && 
                    c.QuestionId == questionId && 
                    c.UserNTID == "test@bosch.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("game-1", "question-1")]
        [InlineData("game-2", "question-2")]
        [InlineData("game-3", "question-3")]
        public async Task DeleteQuestionAsync_WithDifferentIds_CallsMediatrCorrectly(string gameId, string questionId)
        {
            // Arrange
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<DeleteQuestionCommand>(), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.DeleteQuestionAsync(gameId, questionId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mediatorMock.Verify(m => m.Send(
                It.Is<DeleteQuestionCommand>(c => 
                    c.GameId == gameId && 
                    c.QuestionId == questionId && 
                    c.UserNTID == "test@bosch.com"), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetQuestionsAsync_CallsMediatrWithCorrectUserNTID()
        {
            // Arrange
            var gameId = "game-123";
            var questions = new List<QuestionDto>();

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetAllQuestionQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(questions);

            // Act
            await _controller.GetQuestionsAsync(gameId);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetAllQuestionQuery>(q => q.UserNTID == "test@bosch.com"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_CallsMediatrWithCorrectUserNTID()
        {
            // Arrange
            var gameId = "game-123";
            var questionId = "question-456";
            var questionDto = new QuestionDto { Id = questionId, GameId = gameId, Title = "Test", TimeLimitSeconds = 30 };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetQuestionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(questionDto);

            // Act
            await _controller.GetQuestionByIdAsync(gameId, questionId);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetQuestionByIdQuery>(q => q.UserNTID == "test@bosch.com"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateQuestionAsync_SetsGameIdAndUserNTIDFromContext()
        {
            // Arrange
            var gameId = "game-123";
            var command = new CreateQuestionCommand
            {
                Title = "New Question",
                TimeLimitSeconds = 30
            };
            var newQuestionId = "new-question-id";

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateQuestionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newQuestionId);

            // Act
            await _controller.CreateQuestionAsync(gameId, command);

            // Assert
            // Verify that the controller correctly sets the GameId and UserNTID
            Assert.Equal(gameId, command.GameId);
            Assert.Equal("test@bosch.com", command.UserNTID);
        }
    }
}