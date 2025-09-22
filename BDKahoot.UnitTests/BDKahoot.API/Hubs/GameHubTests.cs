using BDKahoot.API.Constants;
using BDKahoot.API.Hubs;
using BDKahoot.API.Hubs.Managers;
using BDKahoot.API.Hubs.Models.Connections;
using BDKahoot.API.Hubs.Models.Game;
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
    public class GameHubTests
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

        public GameHubTests()
        {
            // Create mocks
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

            // Create Hub context mocks
            _contextMock = new Mock<HubCallerContext>();
            _clientsMock = new Mock<IHubCallerClients>();
            _callerMock = new Mock<ISingleClientProxy>();
            _groupsMock = new Mock<IGroupManager>();

            // Setup Hub context
            _contextMock.Setup(x => x.ConnectionId).Returns("test-connection-id");
            _contextMock.Setup(x => x.UserIdentifier).Returns("test-user-id");
            _contextMock.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Upn, "test@bosch.com")
            })));

            _clientsMock.Setup(x => x.Caller).Returns(_callerMock.Object);

            // Create GameHub instance
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

            // Set Hub context
            _gameHub.Context = _contextMock.Object;
            _gameHub.Clients = _clientsMock.Object;
            _gameHub.Groups = _groupsMock.Object;
        }

        #region CreateGameRoom Tests

        [Fact]
        public async Task CreateGameRoom_WithValidGame_ShouldCreateRoom()
        {
            // Arrange
            var gameId = "test-game-id";
            var game = new Game 
            { 
                Id = gameId, 
                Title = "Test Game",
                HostUserNTID = "test@bosch.com"
            };
            var questions = new List<Question>
            {
                new() { Id = "q1", Title = "Question 1", TimeLimitSeconds = 30 }
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                GameId = gameId
            };

            _unitOfWorkMock.Setup(x => x.Games.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _unitOfWorkMock.Setup(x => x.Questions.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _gameSessionServiceMock.Setup(x => x.CreateGameSessionAsync(gameId, "test-connection-id"))
                .ReturnsAsync(gameSession);
            _gameValidationMock.Setup(x => x.ValidateGameRoomCreation("test-user-id", "test@bosch.com", game))
                .Returns(ValidationResult.Success());
            _blobStorageServiceMock.Setup(x => x.GetFileAsync(gameId))
                .ReturnsAsync((MemoryStream?)null);

            // Act
            await _gameHub.CreateGameRoom(gameId, true);

            // Assert
            _unitOfWorkMock.Verify(x => x.Games.GetByIdAsync(gameId), Times.Once);
            _unitOfWorkMock.Verify(x => x.Questions.GetQuestionsByGameIdAsync(gameId), Times.Once);
            _gameSessionServiceMock.Verify(x => x.CreateGameSessionAsync(gameId, "test-connection-id"), Times.Once);
        }

        [Fact]
        public async Task CreateGameRoom_WithInvalidGame_ShouldSendError()
        {
            // Arrange
            var gameId = "invalid-game-id";
            _unitOfWorkMock.Setup(x => x.Games.GetByIdAsync(gameId))
                .ReturnsAsync((Game?)null);
            _gameValidationMock.Setup(x => x.ValidateGameRoomCreation("test-user-id", "test@bosch.com", null))
                .Returns(ValidationResult.Failure("Game not found"));

            // Act
            await _gameHub.CreateGameRoom(gameId, true);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Game not found"), Times.Once);
        }

        [Fact]
        public async Task CreateGameRoom_WithBackgroundImage_ShouldIncludeBackground()
        {
            // Arrange
            var gameId = "test-game-id";
            var game = new Game 
            { 
                Id = gameId, 
                Title = "Test Game",
                HostUserNTID = "test@bosch.com"
            };
            var questions = new List<Question>
            {
                new() { Id = "q1", Title = "Question 1", TimeLimitSeconds = 30 }
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                GameId = gameId
            };
            var backgroundImageData = "test-image-data"u8.ToArray();
            var backgroundStream = new MemoryStream(backgroundImageData);

            _unitOfWorkMock.Setup(x => x.Games.GetByIdAsync(gameId))
                .ReturnsAsync(game);
            _unitOfWorkMock.Setup(x => x.Questions.GetQuestionsByGameIdAsync(gameId))
                .ReturnsAsync(questions);
            _gameSessionServiceMock.Setup(x => x.CreateGameSessionAsync(gameId, "test-connection-id"))
                .ReturnsAsync(gameSession);
            _gameValidationMock.Setup(x => x.ValidateGameRoomCreation("test-user-id", "test@bosch.com", game))
                .Returns(ValidationResult.Success());
            _blobStorageServiceMock.Setup(x => x.GetFileAsync(gameId))
                .ReturnsAsync(backgroundStream);

            // Act
            await _gameHub.CreateGameRoom(gameId, true);

            // Assert
            _blobStorageServiceMock.Verify(x => x.GetFileAsync(gameId), Times.Once);
            _gameSessionManagerMock.Verify(x => x.CreateHubGameSession(
                gameSession.RoomCode, 
                gameId, 
                It.IsAny<List<Question>>(), 
                "test-connection-id", 
                new GameAudio(),
                true, 
                It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region JoinGame Tests

        [Fact]
        public async Task JoinGame_WithValidInput_ShouldJoinSuccessfully()
        {
            // Arrange
            var roomCode = "ABC123";
            var userName = "TestUser";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.Lobby,
                Players = new List<Player>()
            };
            var player = new Player
            {
                UserId = "player-id",
                UserName = userName,
                ConnectionId = "test-connection-id"
            };

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);
            _playerManagerMock.Setup(x => x.ValidatePlayerJoin(gameSession, userName, "test-connection-id", null))
                .ReturnsAsync(new PlayerValidationResult { IsReconnection = false });
            _gameValidationMock.Setup(x => x.ValidatePlayerJoin(userName, gameSession, false))
                .Returns(ValidationResult.Success());
            _playerManagerMock.Setup(x => x.CreateNewPlayer(roomCode, userName, "test-connection-id"))
                .ReturnsAsync(player);

            // Act
            await _gameHub.JoinGame(roomCode, userName);

            // Assert
            _gameSessionManagerMock.Verify(x => x.TryGetGameSession(roomCode, out gameSession), Times.Once);
            _playerManagerMock.Verify(x => x.CreateNewPlayer(roomCode, userName, "test-connection-id"), Times.Once);
        }

        [Fact]
        public async Task JoinGame_WithEmptyUserName_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";
            var userName = "";

            // Act
            await _gameHub.JoinGame(roomCode, userName);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Username cannot be empty"), Times.Once);
        }

        [Fact]
        public async Task JoinGame_WithInvalidRoomCode_ShouldSendError()
        {
            // Arrange
            var roomCode = "INVALID";
            var userName = "TestUser";
            GameSession? gameSession = null;

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(false);

            // Act
            await _gameHub.JoinGame(roomCode, userName);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Room not found"), Times.Once);
        }

        [Fact]
        public async Task JoinGame_ReconnectingPlayer_ShouldHandleReconnection()
        {
            // Arrange
            var roomCode = "ABC123";
            var userName = "TestUser";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.InProgress,
                Players = new List<Player>()
            };
            var existingPlayer = new Player
            {
                UserId = "player-id",
                UserName = userName,
                ConnectionId = "old-connection-id",
                IsConnected = false
            };

            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);
            _playerManagerMock.Setup(x => x.ValidatePlayerJoin(gameSession, userName, "test-connection-id", null))
                .ReturnsAsync(new PlayerValidationResult 
                { 
                    IsReconnection = true, 
                    ExistingPlayer = existingPlayer 
                });

            // Act
            await _gameHub.JoinGame(roomCode, userName);

            // Assert
            _playerManagerMock.Verify(x => x.UpdatePlayerConnectionInfo(existingPlayer, "test-connection-id"), Times.Once);
        }

        #endregion

        #region StartGame Tests

        [Fact]
        public async Task StartGame_AsHost_ShouldStartGame()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.Lobby,
                Players = new List<Player>
                {
                    new() { UserId = "player1", UserName = "Player1" }
                }
            };

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(true);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);
            _gameValidationMock.Setup(x => x.ValidateGameStart(gameSession, true))
                .Returns(ValidationResult.Success());

            // Act
            await _gameHub.StartGame(roomCode);

            // Assert
            _gameSessionServiceMock.Verify(x => x.StartGameSessionAsync(roomCode), Times.Once);
            _gameSessionManagerMock.Verify(x => x.UpdateGameState(roomCode, GameSessionState.InProgress), Times.Once);
        }

        [Fact]
        public async Task StartGame_AsNonHost_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(false);

            // Act
            await _gameHub.StartGame(roomCode);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Only the host can start the game"), Times.Once);
        }

        [Fact]
        public async Task StartGame_WithInvalidGameState_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                State = GameSessionState.InProgress,
                Players = new List<Player>()
            };

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(true);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);
            _gameValidationMock.Setup(x => x.ValidateGameStart(gameSession, true))
                .Returns(ValidationResult.Failure("Game already started"));

            // Act
            await _gameHub.StartGame(roomCode);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Game already started"), Times.Once);
        }

        #endregion

        #region SubmitAnswer Tests

        [Fact]
        public async Task SubmitAnswer_WithValidAnswer_ShouldProcessAnswer()
        {
            // Arrange
            var answerId = "answer-id";
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "test-connection-id",
                RoomCode = "ABC123",
                UserId = "player-id",
                UserName = "TestUser",
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
                    new() { UserId = "player-id", UserName = "TestUser", HasAnswered = false }
                }
            };
            var answer = new Answer { Id = answerId, IsCorrect = true };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("test-connection-id"))
                .Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession))
                .Returns(true);
            _unitOfWorkMock.Setup(x => x.Answers.GetByIdAsync(answerId))
                .ReturnsAsync(answer);

            // Act
            await _gameHub.SubmitAnswer(answerId);

            // Assert
            _unitOfWorkMock.Verify(x => x.Answers.GetByIdAsync(answerId), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_WithoutConnection_ShouldSendError()
        {
            // Arrange
            var answerId = "answer-id";

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("test-connection-id"))
                .Returns((ConnectionInfo?)null);

            // Act
            await _gameHub.SubmitAnswer(answerId);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "You are not in a game"), Times.Once);
        }

        [Fact]
        public async Task SubmitAnswer_GameNotActive_ShouldSendError()
        {
            // Arrange
            var answerId = "answer-id";
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "test-connection-id",
                RoomCode = "ABC123",
                UserId = "player-id",
                UserName = "player1",
                IsHost = false
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                State = GameSessionState.Lobby,
                CurrentQuestionIndex = -1
            };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("test-connection-id"))
                .Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession))
                .Returns(true);

            // Act
            await _gameHub.SubmitAnswer(answerId);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Game not active"), Times.Once);
        }

        #endregion

        #region ProceedToNextQuestion Tests

        [Fact]
        public async Task ProceedToNextQuestion_AsHost_ShouldProceed()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                IsWaitingForHost = true,
                CurrentQuestionIndex = 0,
                Questions = new List<Question>
                {
                    new() { Id = "q1" },
                    new() { Id = "q2" }
                }
            };

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(true);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);

            // Act
            await _gameHub.ProceedToNextQuestion(roomCode);

            // Assert
            gameSession.IsWaitingForHost.Should().BeFalse();
            _gameSessionManagerMock.Verify(x => x.UpdateGameState(roomCode, GameSessionState.InProgress), Times.Once);
        }

        [Fact]
        public async Task ProceedToNextQuestion_AsNonHost_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(false);

            // Act
            await _gameHub.ProceedToNextQuestion(roomCode);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Only the host can proceed to the next question"), Times.Once);
        }

        [Fact]
        public async Task ProceedToNextQuestion_NotWaitingForHost_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                IsWaitingForHost = false
            };

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(true);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);

            // Act
            await _gameHub.ProceedToNextQuestion(roomCode);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Game is not waiting for host input"), Times.Once);
        }

        [Fact]
        public async Task ProceedToNextQuestion_LastQuestion_ShouldShowFinalLeaderboard()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                IsWaitingForHost = true,
                CurrentQuestionIndex = 0,
                Questions = new List<Question>
                {
                    new() { Id = "q1" } // Only one question
                }
            };

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(true);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);

            // Act
            await _gameHub.ProceedToNextQuestion(roomCode);

            // Assert
            gameSession.IsWaitingForHost.Should().BeTrue();
            _gameSessionManagerMock.Verify(x => x.UpdateGameState(roomCode, GameSessionState.WaitingForHost), Times.Once);
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.QuestionResults, 
                It.Is<QuestionResultsResponse>(qr => qr.ShowFinalLeaderboardReady)), Times.Once);
        }

        #endregion

        #region ShowFinalLeaderboard Tests

        [Fact]
        public async Task ShowFinalLeaderboard_AsNonHost_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(false);

            // Act
            await _gameHub.ShowFinalLeaderboard(roomCode);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Only the host can show final leaderboard"), Times.Once);
        }

        [Fact]
        public async Task ShowFinalLeaderboard_NotWaitingForHost_ShouldSendError()
        {
            // Arrange
            var roomCode = "ABC123";
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                IsWaitingForHost = false
            };

            _connectionManagerMock.Setup(x => x.IsHost("test-connection-id"))
                .Returns(true);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession(roomCode, out gameSession))
                .Returns(true);

            // Act
            await _gameHub.ShowFinalLeaderboard(roomCode);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToCallerSafe(
                It.IsAny<IClientProxy>(), 
                SignalREvents.Error, 
                "Game is not waiting for host input"), Times.Once);
        }

        #endregion

        #region Disconnection Tests

        [Fact]
        public async Task OnDisconnectedAsync_WithValidConnection_ShouldCleanup()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "test-connection-id",
                RoomCode = "ABC123",
                UserId = "user-id",
                UserName = "player1",
                IsHost = false
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                Players = new List<Player>
                {
                    new() { UserId = "user-id", ConnectionId = "test-connection-id" }
                }
            };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("test-connection-id"))
                .Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession))
                .Returns(true);

            // Act
            await _gameHub.OnDisconnectedAsync(null);

            // Assert
            _connectionManagerMock.Verify(x => x.CleanupConnection("test-connection-id"), Times.Once);
            _playerManagerMock.Verify(x => x.MarkPlayerAsDisconnected(It.IsAny<Player>()), Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_HostDisconnection_ShouldEndGame()
        {
            // Arrange
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = "test-connection-id",
                RoomCode = "ABC123",
                UserId = "host-id",
                UserName = "player1",
                IsHost = true
            };
            var gameSession = new GameSession
            {
                RoomCode = "ABC123",
                State = GameSessionState.InProgress
            };

            _connectionManagerMock.Setup(x => x.GetConnectionInfo("test-connection-id"))
                .Returns(connectionInfo);
            _gameSessionManagerMock.Setup(x => x.TryGetGameSession("ABC123", out gameSession))
                .Returns(true);

            // Act
            await _gameHub.OnDisconnectedAsync(null);

            // Assert
            _safeCommunicationMock.Verify(x => x.SendToGroupSafe(
                "ABC123",
                SignalREvents.HostDisconnected,
                It.IsAny<HostDisconnectedResponse>()), Times.Once);
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Act & Assert
            _gameHub.Should().NotBeNull();
            _gameHub.Context.Should().NotBeNull();
            _gameHub.Clients.Should().NotBeNull();
            _gameHub.Groups.Should().NotBeNull();
        }

        #endregion
    }
}
