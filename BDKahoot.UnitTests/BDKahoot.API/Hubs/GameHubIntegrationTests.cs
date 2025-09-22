using BDKahoot.API.Constants;
using BDKahoot.API.Hubs;
using BDKahoot.API.Hubs.Managers;
using BDKahoot.API.Hubs.Models.Connections;
using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.API.Hubs.Models.Responses;
using BDKahoot.API.Hubs.Models.Validation;
using BDKahoot.API.Hubs.Services;
using BDKahoot.Application.Services.AnalyticsService;
using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Application.Services.GameSessionService;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace BDKahoot.UnitTests.BDKahoot.API.Hubs
{
    public class GameHubIntegrationTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGameSessionService> _gameSessionServiceMock;
        private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
        private readonly Mock<IAnalyticsService> _analyticsServiceMock;
        private readonly Mock<ILogger<GameHub>> _loggerMock;
        private readonly Mock<IConnectionManager> _connectionManagerMock;
        private readonly Mock<IGameSessionManager> _gameSessionManagerMock;
        private readonly Mock<IPlayerManager> _playerManagerMock;
        private readonly Mock<ISafeCommunicationService> _safeCommunicationMock;
        private readonly Mock<IGameValidationService> _gameValidationMock;
        private readonly Mock<HubCallerContext> _contextMock;
        private readonly Mock<IHubCallerClients> _clientsMock;
        private readonly Mock<ISingleClientProxy> _callerMock;
        private readonly Mock<IGroupManager> _groupsMock;
        private readonly GameHub _gameHub;

        public GameHubIntegrationTests()
        {
            // Setup all mocks similar to main test class
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _gameSessionServiceMock = new Mock<IGameSessionService>();
            _blobStorageServiceMock = new Mock<IBlobStorageService>();
            _analyticsServiceMock = new Mock<IAnalyticsService>();
            _loggerMock = new Mock<ILogger<GameHub>>();
            _connectionManagerMock = new Mock<IConnectionManager>();
            _gameSessionManagerMock = new Mock<IGameSessionManager>();
            _playerManagerMock = new Mock<IPlayerManager>();
            _safeCommunicationMock = new Mock<ISafeCommunicationService>();
            _gameValidationMock = new Mock<IGameValidationService>();

            _contextMock = new Mock<HubCallerContext>();
            _clientsMock = new Mock<IHubCallerClients>();
            _callerMock = new Mock<ISingleClientProxy>();
            _groupsMock = new Mock<IGroupManager>();

            _contextMock.Setup(x => x.ConnectionId).Returns("test-connection-id");
            _contextMock.Setup(x => x.UserIdentifier).Returns("test-user-id");
            _contextMock.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("upn", "test@bosch.com")
            })));

            _clientsMock.Setup(x => x.Caller).Returns(_callerMock.Object);

            _gameHub = new GameHub(
                _unitOfWorkMock.Object,
                _gameSessionServiceMock.Object,
                _blobStorageServiceMock.Object,
                _analyticsServiceMock.Object,
                _loggerMock.Object,
                _connectionManagerMock.Object,
                _gameSessionManagerMock.Object,
                _playerManagerMock.Object,
                _safeCommunicationMock.Object,
                _gameValidationMock.Object
            );

            _gameHub.Context = _contextMock.Object;
            _gameHub.Clients = _clientsMock.Object;
            _gameHub.Groups = _groupsMock.Object;
        }

        #region Complete Game Flow Tests

        [Fact]
        public async Task CompleteGameFlow_FromCreateToFinish_ShouldExecuteCorrectly()
        {
            // Arrange - Create Game Room
            var gameId = "test-game-id";
            var roomCode = "ABC123";
            var game = new Game
            {
                Id = gameId,
                Title = "Test Game",
                HostUserNTID = "test@bosch.com"
            };
            var questions = new List<Question>
            {
                new() { Id = "q1", Title = "Question 1", TimeLimitSeconds = 30, Type = QuestionType.SingleChoice },
                new() { Id = "q2", Title = "Question 2", TimeLimitSeconds = 30, Type = QuestionType.MultipleChoice }
            };
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                GameId = gameId,
                State = GameSessionState.Lobby,
                Questions = questions,
                Players = new List<Player>()
            };

            // Setup Create Game Room
            _unitOfWorkMock.Setup(x => x.Games.GetByIdAsync(gameId)).ReturnsAsync(game);
            _unitOfWorkMock.Setup(x => x.Questions.GetQuestionsByGameIdAsync(gameId)).ReturnsAsync(questions);
            _gameSessionServiceMock.Setup(x => x.CreateGameSessionAsync(gameId, "test-connection-id")).ReturnsAsync(gameSession);
            _gameValidationMock.Setup(x => x.ValidateGameRoomCreation("test-user-id", "test@bosch.com", game))
                .Returns(ValidationResult.Success());
            _blobStorageServiceMock.Setup(x => x.GetFileAsync(gameId)).ReturnsAsync((MemoryStream?)null);

            // Act 1 - Create Game Room
            await _gameHub.CreateGameRoom(gameId, true);

            // Arrange - Join Game
            var player1 = new Player
            {
                UserId = "player1-id",
                UserName = "Player1",
                ConnectionId = "player1-connection"
            };

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession)).Returns(true);
            _playerManagerMock.Setup(x => x.ValidatePlayerJoin(gameSession, "Player1", "player1-connection", null))
                .ReturnsAsync(new PlayerValidationResult { IsReconnection = false });
            _gameValidationMock.Setup(x => x.ValidatePlayerJoin("Player1", gameSession, false))
                .Returns(ValidationResult.Success());
            _playerManagerMock.Setup(x => x.CreateNewPlayer(roomCode, "Player1", "player1-connection"))
                .ReturnsAsync(player1);

            // Switch context to player
            _contextMock.Setup(x => x.ConnectionId).Returns("player1-connection");

            // Act 2 - Player Joins
            await _gameHub.JoinGame(roomCode, "Player1");

            // Assert 2 - Player Joined
            _playerManagerMock.Verify(x => x.CreateNewPlayer(roomCode, "Player1", "player1-connection"), Times.Once);

            // Arrange - Start Game
            gameSession.Players.Add(player1);
            _contextMock.Setup(x => x.ConnectionId).Returns("test-connection-id"); // Back to host
            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id")).Returns(true);
            _gameValidationMock.Setup(x => x.ValidateGameStart(gameSession, true))
                .Returns(ValidationResult.Success());

            // Act 3 - Start Game
            await _gameHub.StartGame(roomCode);

            // Assert 3 - Game Started
            _gameSessionServiceMock.Verify(x => x.StartGameSessionAsync(roomCode), Times.Once);
        }

        [Fact]
        public async Task MultiplePlayersFlow_ShouldHandleCorrectly()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.Lobby,
                Players = new List<Player>()
            };

            var players = new List<Player>
            {
                new() { UserId = "p1", UserName = "Player1", ConnectionId = "conn1" },
                new() { UserId = "p2", UserName = "Player2", ConnectionId = "conn2" },
                new() { UserId = "p3", UserName = "Player3", ConnectionId = "conn3" }
            };

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession)).Returns(true);

            foreach (var player in players)
            {
                _playerManagerMock.Setup(x => x.ValidatePlayerJoin(gameSession, player.UserName, player.ConnectionId, null))
                    .ReturnsAsync(new PlayerValidationResult { IsReconnection = false });
                _gameValidationMock.Setup(x => x.ValidatePlayerJoin(player.UserName, gameSession, false))
                    .Returns(ValidationResult.Success());
                _playerManagerMock.Setup(x => x.CreateNewPlayer(roomCode, player.UserName, player.ConnectionId))
                    .ReturnsAsync(player);
            }

            // Act - All players join
            foreach (var player in players)
            {
                _contextMock.Setup(x => x.ConnectionId).Returns(player.ConnectionId);
                await _gameHub.JoinGame(roomCode, player.UserName);
                gameSession.Players.Add(player);
            }

            // Assert
            _playerManagerMock.Verify(x => x.CreateNewPlayer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
        }

        #endregion

        #region Question Answering Scenarios

        [Fact]
        public async Task QuestionAnswering_SingleChoice_ShouldProcessCorrectly()
        {
            // Arrange
            var answerId = "correct-answer";
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "player1-conn",
                RoomCode = "ABC123",
                UserId = "player1",
                UserName = "Player1",
                IsHost = false
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                State = GameSessionState.InProgress,
                CurrentQuestionIndex = 0,
                Questions = new List<Question>
                {
                    new() { Id = "q1", Type = QuestionType.SingleChoice }
                },
                Players = new List<Player>
                {
                    new() { UserId = "player1", UserName = "Player1", HasAnswered = false, LastAnswerIds = new List<string>() }
                }
            };
            var answer = new Answer { Id = answerId, IsCorrect = true };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("player1-conn")).Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession)).Returns(true);
            _unitOfWorkMock.Setup(x => x.Answers.GetByIdAsync(answerId)).ReturnsAsync(answer);

            _contextMock.Setup(x => x.ConnectionId).Returns("player1-conn");

            // Act
            await _gameHub.SubmitAnswer(answerId);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(),
                SignalREvents.AnswerSubmitted,
                It.IsAny<AnswerSubmittedResponse>()), Times.Once);
        }

        [Fact]
        public async Task QuestionAnswering_MultipleChoice_ShouldAllowMultipleAnswers()
        {
            // Arrange
            var answerId1 = "answer1";
            var answerId2 = "answer2";
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "player1-conn",
                RoomCode = "ABC123",
                UserId = "player1",
                UserName = "Player1",
                IsHost = false
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                State = GameSessionState.InProgress,
                CurrentQuestionIndex = 0,
                Questions = new List<Question>
                {
                    new() { Id = "q1", Type = QuestionType.MultipleChoice }
                },
                Players = new List<Player>
                {
                    new() { UserId = "player1", UserName = "Player1", HasAnswered = false, LastAnswerIds = new List<string>() }
                }
            };
            var answer1 = new Answer { Id = answerId1, IsCorrect = true };
            var answer2 = new Answer { Id = answerId2, IsCorrect = true };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("player1-conn")).Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession)).Returns(true);
            _unitOfWorkMock.Setup(x => x.Answers.GetByIdAsync(answerId1)).ReturnsAsync(answer1);
            _unitOfWorkMock.Setup(x => x.Answers.GetByIdAsync(answerId2)).ReturnsAsync(answer2);

            _contextMock.Setup(x => x.ConnectionId).Returns("player1-conn");

            // Act - Submit multiple answers
            await _gameHub.SubmitAnswer(answerId1);
            await _gameHub.SubmitAnswer(answerId2);

            // Assert
            _unitOfWorkMock.Verify(x => x.Answers.GetByIdAsync(answerId1), Times.Once);
            _unitOfWorkMock.Verify(x => x.Answers.GetByIdAsync(answerId2), Times.Once);
        }

        #endregion

        #region Reconnection Scenarios

        [Fact]
        public async Task PlayerReconnection_DuringGame_ShouldRestoreState()
        {
            // Arrange
            var roomCode = "ABC123";
            var existingPlayer = new Player
            {
                UserId = "player1",
                UserName = "Player1",
                ConnectionId = "old-connection",
                IsConnected = false,
                Score = 100,
                HasAnswered = true,
                CurrentQuestionIndex = 1
            };
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.InProgress,
                CurrentQuestionIndex = 1,
                Players = new List<Player> { existingPlayer },
                Questions = new List<Question>
                {
                    new() { Id = "q1" },
                    new() { Id = "q2" }
                }
            };

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession)).Returns(true);
            _playerManagerMock.Setup(x => x.ValidatePlayerJoin(gameSession, "Player1", "new-connection", null))
                .ReturnsAsync(new PlayerValidationResult
                {
                    IsReconnection = true,
                    ExistingPlayer = existingPlayer
                });

            _contextMock.Setup(x => x.ConnectionId).Returns("new-connection");

            // Act
            await _gameHub.JoinGame(roomCode, "Player1");

            // Assert
            _playerManagerMock.Verify(x => x.UpdatePlayerConnectionInfo(existingPlayer, "new-connection"), Times.Once);
            existingPlayer.IsConnected.Should().BeTrue();
        }

        [Fact]
        public async Task HostDisconnection_DuringGame_ShouldEndGame()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "host-connection",
                RoomCode = "ABC123",
                UserId = "host-id",
                UserName = "Host",
                IsHost = true
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                State = GameSessionState.InProgress,
                Players = new List<Player>
                {
                    new() { UserId = "player1", UserName = "Player1" }
                }
            };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("host-connection")).Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession)).Returns(true);

            _contextMock.Setup(x => x.ConnectionId).Returns("host-connection");

            // Act
            await _gameHub.OnDisconnectedAsync(null);

            // Assert
            _connectionManagerMock.Verify(x => x.CleanupConnection("host-connection"), Times.Once);
            _safeCommunicationMock.Verify(x => x.SendToGroupSafe(
                "ABC123",
                SignalREvents.HostDisconnected,
                It.IsAny<HostDisconnectedResponse>()), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task CreateGameRoom_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var gameId = "test-game-id";
            _unitOfWorkMock.Setup(x => x.Games.GetByIdAsync(gameId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            await _gameHub.CreateGameRoom(gameId, true);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(),
                SignalREvents.Error,
                "Failed to create game room"), Times.Once);
        }

        [Fact]
        public async Task JoinGame_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var roomCode = "ABC123";
            var userName = "TestUser";
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(It.IsAny<string>(), out It.Ref<GameSession?>.IsAny))
                .Throws(new Exception("Session manager error"));

            // Act
            await _gameHub.JoinGame(roomCode, userName);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(),
                SignalREvents.Error,
                "Failed to join game"), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_WithException_ShouldHandleGracefully()
        {
            // Arrange
            var answerId = "answer-id";
            _connectionManagerMock.Setup(x => x.GetConnectionInfo(It.IsAny<string>()))
                .Throws(new Exception("Connection manager error"));

            // Act
            await _gameHub.SubmitAnswer(answerId);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(),
                SignalREvents.Error,
                "Failed to submit answer"), Times.Once);
        }

        #endregion

        #region Background Image Tests

        #endregion

        #region Performance and Stress Tests

        [Fact]
        public async Task ManyPlayersJoinSimultaneously_ShouldHandleCorrectly()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.Lobby,
                Players = new List<Player>()
            };
            var playerCount = 50;

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession)).Returns(true);

            for (int i = 0; i < playerCount; i++)
            {
                var playerName = $"Player{i}";
                var connectionId = $"conn{i}";
                var player = new Player { UserId = $"p{i}", UserName = playerName, ConnectionId = connectionId };

                _playerManagerMock.Setup(x => x.ValidatePlayerJoin(gameSession, playerName, connectionId, null))
                    .ReturnsAsync(new PlayerValidationResult { IsReconnection = false });
                _gameValidationMock.Setup(x => x.ValidatePlayerJoin(playerName, gameSession, false))
                    .Returns(ValidationResult.Success());
                _playerManagerMock.Setup(x => x.CreateNewPlayer(roomCode, playerName, connectionId))
                    .ReturnsAsync(player);
            }

            // Act
            var tasks = new List<Task>();
            for (int i = 0; i < playerCount; i++)
            {
                var playerName = $"Player{i}";
                var connectionId = $"conn{i}";
                _contextMock.Setup(x => x.ConnectionId).Returns(connectionId);

                tasks.Add(_gameHub.JoinGame(roomCode, playerName));
            }

            await Task.WhenAll(tasks);

            // Assert
            _playerManagerMock.Verify(x => x.CreateNewPlayer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(playerCount));
        }

        #endregion
    }
}
