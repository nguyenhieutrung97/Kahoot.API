using BDKahoot.API.Constants;
using BDKahoot.API.Hubs.Managers;
using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.API.Hubs.Models.Responses;
using BDKahoot.API.Hubs.Services;
using BDKahoot.Application.Extensions;
using BDKahoot.Application.Services.AnalyticsService;
//using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Application.Services.GameSessionService;
using BDKahoot.Domain.Enums;
using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HubConnectionInfo = BDKahoot.API.Hubs.Models.Connections.ConnectionInfo;

namespace BDKahoot.API.Hubs
{
    /// <summary>
    /// Advanced implementation of Game Hub for Kahoot-like functionality 
    /// </summary>
    public class GameHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGameSessionService _gameSessionService;
        //private readonly IBlobStorageService _blobStorageService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<GameHub> _logger;
        private readonly IConnectionManager _connectionManager;
        private readonly IGameSessionManager _gameSessionManager;
        private readonly IPlayerManager _playerManager;
        private readonly ISafeCommunicationService _safeCommunication;
        private readonly IGameValidationService _gameValidation;

        public GameHub(IUnitOfWork unitOfWork, IGameSessionService gameSessionService,IAnalyticsService analyticsService,
            ILogger<GameHub> logger, IConnectionManager connectionManager, IGameSessionManager gameSessionManager, IPlayerManager playerManager,
            ISafeCommunicationService safeCommunication, IGameValidationService gameValidation)
        {
            _unitOfWork = unitOfWork;
            _gameSessionService = gameSessionService;
            //_blobStorageService = blobStorageService;
            _analyticsService = analyticsService;
            _logger = logger;
            _connectionManager = connectionManager;
            _gameSessionManager = gameSessionManager;
            _playerManager = playerManager;
            _safeCommunication = safeCommunication;
            _gameValidation = gameValidation;
        }

        /// <summary>
        /// Handle client disconnection - Clean up connection mappings 
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                _logger.LogInformation($"Connection {connectionId} disconnected. Reason: {exception?.Message ?? "Normal disconnect"}");

                await HandleDisconnection(connectionId);
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling disconnection for {Context.ConnectionId}");
                await base.OnDisconnectedAsync(exception);
            }
        }

        /// <summary>
        /// Create a new game room that players can join 
        /// </summary>
        //[Authorize]
        public async Task CreateGameRoom(string gameId, bool autoShowResults = true)
        {
            try
            {
                var userId = Context.UserIdentifier;
                var userNTID = Context.User?.GetUserNTID();

                // Validate game room creation
                var game = await _unitOfWork.Games.GetByIdAsync(gameId);
                var validation = _gameValidation.ValidateGameRoomCreation(userId, userNTID, game);
                if (!validation.IsSuccess)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, validation.ErrorMessage);
                    return;
                }

                // Create game room
                var roomCode = await CreateGameRoomInternal(gameId, game!, autoShowResults);
                if (roomCode != null)
                {
                    _logger.LogInformation($"Game room created with code {roomCode} for game {gameId} by user {userNTID}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating game room");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to create game room");
            }
        }

        /// <summary>
        /// Join an existing game using the room code 
        /// </summary>
        [AllowAnonymous]
        public async Task JoinGame(string roomCode, string userName, string? playerId = null)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(userName))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Username cannot be empty");
                    return;
                }

                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Room not found");
                    return;
                }

                // Handle player join logic with reconnection support
                var joinResult = await ProcessPlayerJoin(roomCode, userName, gameSession!, playerId);

                if (joinResult.IsSuccess)
                {
                    _logger.LogInformation($"Player {userName} joined room {roomCode} successfully (Reconnecting: {joinResult.IsReconnection})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining game for player {userName} in room {roomCode}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to join game");
            }
        }

        /// <summary>
        /// Start the game session 
        /// </summary>
        //[Authorize]
        public async Task StartGame(string roomCode)
        {
            try
            {
                // Validate host permissions
                if (!IsCallerHost())
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Only the host can start the game");
                    return;
                }

                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Room not found");
                    return;
                }

                // Validate game start conditions
                var validation = _gameValidation.ValidateGameStart(gameSession!, true);
                if (!validation.IsSuccess)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, validation.ErrorMessage);
                    return;
                }

                await StartGameInternal(roomCode, gameSession!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting game for room {roomCode}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to start game");
            }
        }

        /// <summary>
        /// Submit an answer for the current question 
        /// </summary>
        [AllowAnonymous]
        public async Task SubmitAnswer(string answerId)
        {
            try
            {
                // Get connection info
                var connectionInfo = _connectionManager.GetConnectionInfo(Context.ConnectionId);
                if (connectionInfo == null)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "You are not in a game");
                    return;
                }

                var roomCode = connectionInfo.RoomCode;
                var playerId = connectionInfo.UserId;

                // Get the game session
                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Game session not found");
                    return;
                }

                // Validate game state
                if (gameSession!.State != GameSessionState.InProgress || gameSession.CurrentQuestionIndex < 0)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Game not active");
                    return;
                }

                // Find the player
                var player = gameSession.Players.FirstOrDefault(p => p.UserId == playerId);
                if (player == null)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Player not found in this game");
                    return;
                }

                // Get the current question and validate answer
                var question = gameSession.Questions[gameSession.CurrentQuestionIndex];
                var answer = await _unitOfWork.Answers.GetByIdAsync(answerId);
                if (answer == null)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Invalid answer");
                    return;
                }

                _logger.LogInformation($"Player {player.UserName} submitting answer {answerId} for {question.Type} question. Current HasAnswered: {player.HasAnswered}");

                // Handle answer submission based on question type
                bool answerProcessed = await ProcessAnswerSubmission(player, question, answer, gameSession);

                if (answerProcessed)
                {
                    // Send confirmation to player
                    var answerSubmittedResponse = new AnswerSubmittedResponse
                    {
                        AnswerId = answerId,
                        QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                        IsMultipleChoice = question.Type == QuestionType.MultipleChoice,
                        SelectedAnswers = player.LastAnswerIds.ToList()
                    };
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.AnswerSubmitted, answerSubmittedResponse);

                    // Update host with current progress
                    await SendProgressUpdateToHost(roomCode, gameSession);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting answer {answerId}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to submit answer");
            }
        }

        /// <summary>
        /// Submit multiple answers for MultipleChoice questions 
        /// </summary>
        [AllowAnonymous]
        public async Task SubmitMultipleAnswers(List<string> answerIds)
        {
            try
            {
                // Get connection info
                var connectionInfo = _connectionManager.GetConnectionInfo(Context.ConnectionId);
                if (connectionInfo == null)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "You are not in a game");
                    return;
                }

                var roomCode = connectionInfo.RoomCode;
                var playerId = connectionInfo.UserId;

                // Get the game session
                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Game session not found");
                    return;
                }

                // Validate game state
                if (gameSession!.State != GameSessionState.InProgress || gameSession.CurrentQuestionIndex < 0)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Game not active");
                    return;
                }

                // Find the player
                var player = gameSession.Players.FirstOrDefault(p => p.UserId == playerId);
                if (player == null)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Player not found in this game");
                    return;
                }

                // Get the current question and validate it's MultipleChoice
                var question = gameSession.Questions[gameSession.CurrentQuestionIndex];
                if (question.Type != QuestionType.MultipleChoice)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "This method is only for MultipleChoice questions");
                    return;
                }

                // Validate answers exist
                if (answerIds == null || !answerIds.Any())
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Please select at least one answer");
                    return;
                }

                // Check if player already finalized their answers
                if (player.HasAnswered && player.CurrentQuestionIndex == gameSession.CurrentQuestionIndex)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "You have already finalized your answers for this question");
                    return;
                }

                _logger.LogInformation($"Player {player.UserName} submitting {answerIds.Count} multiple answers for MultipleChoice question");

                // Validate all answers exist
                var validAnswers = new List<Answer>();
                foreach (var answerId in answerIds)
                {
                    var answer = await _unitOfWork.Answers.GetByIdAsync(answerId);
                    if (answer == null)
                    {
                        await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, $"Invalid answer ID: {answerId}");
                        return;
                    }
                    validAnswers.Add(answer);
                }

                // Set all selected answers and mark as answered
                player.LastAnswerIds.Clear();
                player.LastAnswerIds.AddRange(answerIds);
                player.HasAnswered = true;
                player.CurrentQuestionIndex = gameSession.CurrentQuestionIndex;

                // Calculate score for multiple choice
                await CalculateAndUpdatePlayerScore(player, gameSession);

                // Send confirmation to player
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.MultipleAnswersSubmitted, new MultipleAnswersSubmittedResponse
                {
                    AnswerIds = answerIds,
                    QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                    IsFinalized = true,
                    SelectedAnswers = player.LastAnswerIds.ToList()
                });

                // Update host with current progress
                await SendProgressUpdateToHost(roomCode, gameSession);

                _logger.LogInformation($"Player {player.UserName} successfully submitted {answerIds.Count} multiple answers for question {gameSession.CurrentQuestionIndex + 1}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting multiple answers");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to submit multiple answers");
            }
        }

        /// <summary>
        /// Host proceeds to next question 
        /// </summary>
        //[Authorize]
        public async Task ProceedToNextQuestion(string roomCode)
        {
            try
            {
                // Validate host permissions
                if (!IsCallerHost())
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Only the host can proceed to the next question");
                    return;
                }

                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Room not found");
                    return;
                }

                if (!gameSession!.IsWaitingForHost)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Game is not waiting for host input");
                    return;
                }

                _logger.LogInformation($"Host proceeding to next question for room {roomCode}");

                // Reset waiting state
                gameSession.IsWaitingForHost = false;
                _gameSessionManager.UpdateGameState(roomCode, GameSessionState.InProgress);

                // Check if this was the last question
                bool isLastQuestion = gameSession.CurrentQuestionIndex >= gameSession.Questions.Count - 1;

                if (isLastQuestion)
                {
                    _logger.LogInformation($"Last question completed for room {roomCode}. Waiting for host to show final leaderboard.");

                    gameSession.IsWaitingForHost = true;
                    _gameSessionManager.UpdateGameState(roomCode, GameSessionState.WaitingForHost);

                    // Notify host that they can now show final leaderboard
                    var questionResultsResponse = new QuestionResultsResponse
                    {
                        QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                        TotalQuestions = gameSession.Questions.Count,
                        IsLastQuestion = true,
                        Message = "This was the last question! You can now show the final leaderboard.",
                        ShowFinalLeaderboardReady = true
                    };
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.QuestionResults, questionResultsResponse);

                    return;
                }

                // Notify all players that we're proceeding to next question
                var proceedResponse = new ProceedToNextQuestionResponse
                {
                    Message = "Moving to next question...",
                    CurrentQuestionIndex = gameSession.CurrentQuestionIndex + 1,
                    TotalQuestions = gameSession.Questions.Count
                };
                await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.ProceedingToNextQuestion, proceedResponse);

                // Proceed to next question
                await SendNextQuestion(roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error proceeding to next question for room {roomCode}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to proceed to next question");
            }
        }

        /// <summary>
        /// Show final leaderboard manually by host 
        /// </summary>
        //[Authorize]
        public async Task ShowFinalLeaderboard(string roomCode)
        {
            try
            {
                // Validate host permissions
                if (!IsCallerHost())
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Only the host can show final leaderboard");
                    return;
                }

                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Room not found");
                    return;
                }

                if (!gameSession!.IsWaitingForHost)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Game is not waiting for host input");
                    return;
                }

                _logger.LogInformation($"Host showing final leaderboard for room {roomCode}");

                // End the game
                await EndGameSafe(roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing final leaderboard for room {roomCode}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to show final leaderboard");
            }
        }

        //[Authorize]
        public async Task KickPlayer(string roomCode, string playerId)
        {
            // Validate host permissions
            if (!IsCallerHost())
            {
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Only the host can kick players");
                return;
            }

            if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
            {
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Room not found");
                return;
            }

            // Validate kick conditions (can only kick in lobby)
            var validation = _gameValidation.ValidatePlayerKick(gameSession!, true);
            if (!validation.IsSuccess)
            {
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, validation.ErrorMessage);
                return;
            }

            var player = gameSession!.Players.FirstOrDefault(p => p.UserId == playerId);
            await PlayerLeave(gameSession!, player, true);
        }

        #region Private Helper Methods

        private async Task PlayerLeave(GameSession gameSession, Player? player, bool wasKicked = false)
        {
            var roomCode = gameSession.Id;

            // Find the player to kick/leave
            if (player == null)
            {
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Player not found in this game");
                return;
            }

            // Save player connection ID and username before removing from game
            string playerConnectionId = player.ConnectionId;
            string playerUserName = player.UserName;
            string playerId = player.UserId;

            // Remove player from SignalR group
            await Groups.RemoveFromGroupAsync(playerConnectionId, roomCode);

            // Clean up connection mappings 
            _connectionManager.CleanupPlayerConnection(playerConnectionId, playerUserName);

            if (wasKicked)
            {
                // Send kick notification to the player being kicked
                await _safeCommunication.SendToClientSafe(playerConnectionId, SignalREvents.KickedFromGame,
                    "You have been kicked from the game by the host");
            }

            // Remove player from memory session
            gameSession.Players.RemoveAll(p => p.UserId == playerId);

            // Create updated player list data for notifications
            var updatedPlayerList = _playerManager.CreatePlayerListData(gameSession.Players);

            // Send kick notification to other players and host
            var playerLeftData = new PlayerLeftResponse
            {
                PlayerId = playerId,
                UserName = playerUserName,
                WasKicked = true,
                TotalPlayers = gameSession.Players.Count,
                Players = updatedPlayerList
            };

            // Send to others player
            await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.PlayerLeft, playerLeftData);

            // Send to host
            await SendLobbyInfoToHost(roomCode, gameSession);

            // Remove player from database
            await _playerManager.RemovePlayerFromSession(roomCode, playerId);

            if (wasKicked)
            {
                _logger.LogInformation($"Player {playerUserName} was kicked from room {roomCode} by host");
            }
        }

        /// <summary>
        /// Handle disconnection logic separated from OnDisconnectedAsync
        /// </summary>
        private async Task HandleDisconnection(string connectionId)
        {
            var connectionInfo = _connectionManager.GetConnectionInfo(connectionId);
            if (connectionInfo == null) return;

            if (!_gameSessionManager.TryGetGameSession(connectionInfo.RoomCode, out var gameSession))
            {
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Room not found");
                return;
            }

            // If in Lobby, just remove player. If in-game, mark as disconnected (allow player re-connect).
            if (gameSession!.State == GameSessionState.Lobby)
            {
                var player = gameSession.Players.FirstOrDefault(p => p.ConnectionId == connectionInfo.ConnectionId);
                await PlayerLeave(gameSession!, player);
                return;
            }
            else
            {
                // Clean up connection mappings
                _connectionManager.CleanupConnection(connectionId);

                if (connectionInfo.IsHost)
                {
                    await HandleHostDisconnection(connectionInfo.RoomCode);
                }
                else
                {
                    await HandlePlayerDisconnection(connectionInfo, gameSession);
                }
            }
        }

        /// <summary>
        /// Handle host disconnection
        /// </summary>
        private async Task HandleHostDisconnection(string roomCode)
        {
            _logger.LogWarning($"Host disconnected from room {roomCode}. Ending game session.");

            var hostDisconnectedResponse = new HostDisconnectedResponse
            {
                Message = "The host has disconnected. The game will end.",
                RoomCode = roomCode
            };
            await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.HostDisconnected, hostDisconnectedResponse);

            // Record analytics for aborted session before ending
            try
            {
                if (_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    // Only record if the game was actually in progress (not just in lobby)
                    if (gameSession!.State == GameSessionState.InProgress)
                    {
                        // Record as aborted instead of completed since host disconnected
                        await _gameSessionService.EndGameSessionAsync(roomCode, true);

                        // Try to save partial session analytics for aborted games
                        var dbGameSession = await _unitOfWork.GameSessions.GetByRoomCodeAsync(roomCode);
                        if (dbGameSession != null)
                        {
                            var allPlayers = await _unitOfWork.Players.GetByGameSessionIdAsync(dbGameSession.Id!);
                            if (allPlayers.Any())
                            {
                                // Mark all players as disconnected due to abortion
                                var modifiedHubPlayers = gameSession.Players.Select(hp =>
                                {
                                    hp.IsConnected = false; // Mark as disconnected due to abortion
                                    return hp;
                                }).ToList();

                                await SyncPlayerDataToDatabase(modifiedHubPlayers, allPlayers);
                                await _analyticsService.CreateSessionAnalyticsAsync(dbGameSession, allPlayers);

                                _logger.LogInformation($"Updated analytics created for aborted session {roomCode}");
                            }
                        }
                    }
                }
            }
            catch (Exception analyticsEx)
            {
                _logger.LogError(analyticsEx, $"Error recording analytics for host disconnected session {roomCode}");
            }

            await EndGameSafe(roomCode);
            _gameSessionManager.RemoveGameSession(roomCode);
        }

        /// <summary>
        /// Handle player disconnection
        /// </summary>
        private async Task HandlePlayerDisconnection(HubConnectionInfo connectionInfo, GameSession gameSession)
        {
            var player = gameSession.Players.FirstOrDefault(p => p.ConnectionId == connectionInfo.ConnectionId);
            if (player != null)
            {
                _playerManager.MarkPlayerAsDisconnected(player);
                _logger.LogInformation($"Player {connectionInfo.UserName} marked as disconnected from room {connectionInfo.RoomCode}");

                // Record player disconnection for analytics (could be useful to track disconnect rates)
                try
                {
                    _logger.LogInformation($"Player {connectionInfo.UserName} disconnected from game in progress. " +
                                         $"Game state: {gameSession.State}, Players connected: {gameSession.Players.Count(p => p.IsConnected)}/{gameSession.Players.Count}");
                }
                catch (Exception analyticsEx)
                {
                    _logger.LogError(analyticsEx, $"Error logging disconnection analytics for player {connectionInfo.UserName}");
                }

                var playerDisconnectedResponse = new PlayerDisconnectedResponse
                {
                    PlayerId = connectionInfo.UserId,
                    UserName = connectionInfo.UserName,
                    TotalPlayers = gameSession.Players.Count,
                    ConnectedPlayers = gameSession.Players.Count(p => p.IsConnected)
                };
                await _safeCommunication.SendToGroupSafe(connectionInfo.RoomCode, SignalREvents.PlayerDisconnected, playerDisconnectedResponse);

                if (gameSession.State == GameSessionState.Lobby)
                {
                    await SendLobbyInfoToHost(connectionInfo.RoomCode, gameSession);
                }
            }
        }

        /// <summary>
        /// Create game room internal implementation
        /// </summary>
        private async Task<string?> CreateGameRoomInternal(string gameId, Game game, bool autoShowResults)
        {
            // Get questions
            var questions = await _unitOfWork.Questions.GetQuestionsByGameIdAsync(gameId);
            if (!questions.Any())
            {
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "No questions available for this game");
                return null;
            }

            // Create game session
            var gameSession = await _gameSessionService.CreateGameSessionAsync(gameId, Context.ConnectionId);

            //// Get background image from blob storage (if available)
            //string? backgroundImageBase64 = null;
            //GameAudio gameAudioUrls = new GameAudio();
            //try
            //{
            //    var backgroundStream = await _blobStorageService.GetFileAsync(gameId);
            //    if (backgroundStream != null)
            //    {
            //        backgroundStream.Position = 0; // Reset stream position to start
            //        backgroundImageBase64 = Convert.ToBase64String(backgroundStream.ToArray());
            //        _logger.LogInformation($"Found background image for game {gameId}");
            //    }

            //    gameAudioUrls = await _blobStorageService.GetAudioFileUrlsAsync();
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogWarning(ex, $"Error getting background image for game {gameId}. Will continue without background.");
            //}

            // Create hub game session
            var hubGameSession = _gameSessionManager.CreateHubGameSession(
                gameSession.RoomCode, gameId, questions.ToList(), Context.ConnectionId, autoShowResults);

            _gameSessionManager.AddGameSession(gameSession.RoomCode, hubGameSession);

            // Setup connections and notifications
            _connectionManager.AddConnectionMapping(Context.ConnectionId, gameSession.RoomCode,
                Context.UserIdentifier!, true, "Host");

            await Groups.AddToGroupAsync(Context.ConnectionId, gameSession.RoomCode);

            // Send response
            var roomCreatedResponse = new RoomCreatedResponse
            {
                RoomCode = gameSession.RoomCode,
                GameTitle = game.Title,
                TotalQuestions = questions.Count(),
                TotalPlaytime = questions.Sum(q => q.TimeLimitSeconds),
                AutoShowResults = autoShowResults,
                //GameBackgroundBase64 = backgroundImageBase64,
                //GameAudioUrls = gameAudioUrls
            };
            await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.RoomCreated, roomCreatedResponse);

            await SendLobbyInfoToHost(gameSession.RoomCode, hubGameSession);
            return gameSession.RoomCode;
        }

        /// <summary>
        /// Process player join with reconnection support 
        /// </summary>
        private async Task<PlayerJoinResult> ProcessPlayerJoin(string roomCode, string userName, GameSession gameSession, string? playerId = null)
        {
            try
            {
                // Check for existing player (reconnection scenario) FIRST
                var playerValidation = await _playerManager.ValidatePlayerJoin(gameSession, userName, Context.ConnectionId, playerId);

                Player? player;
                bool isReconnection = false;

                if (playerValidation.ExistingPlayer != null && !playerValidation.IsReconnection)
                {
                    await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Unfortunately that this name is already used. Please input another name.");
                    return PlayerJoinResult.Failed();
                }
                else if (playerValidation.ExistingPlayer != null && playerValidation.IsReconnection)
                {
                    isReconnection = true;
                    player = playerValidation.ExistingPlayer;

                    _logger.LogInformation($"Player {userName} attempting reconnection to room {roomCode}");
                }
                else
                {
                    // For new players, validate join conditions
                    var validation = _gameValidation.ValidatePlayerJoin(userName, gameSession, isReconnection);
                    if (!validation.IsSuccess)
                    {
                        await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, validation.ErrorMessage);
                        return PlayerJoinResult.Failed();
                    }

                    // Create new player
                    player = await _playerManager.CreateNewPlayer(roomCode, userName, Context.ConnectionId);
                    if (player == null)
                    {
                        await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to create player");
                        return PlayerJoinResult.Failed();
                    }
                    gameSession.Players.Add(player);
                    _logger.LogInformation($"New player {userName} joining room {roomCode}");
                }

                // Handle reconnection updates
                if (isReconnection)
                {
                    // Update connection information
                    _playerManager.UpdatePlayerConnectionInfo(player, Context.ConnectionId);
                    player.IsConnected = true; // Mark as reconnected

                    _logger.LogInformation($"Player {userName} reconnecting to room {roomCode} with new connection {Context.ConnectionId}");

                    // Send current game state to reconnecting player
                    await SendReconnectionState(player, gameSession);
                }

                // Setup connection mapping
                _connectionManager.AddConnectionMapping(Context.ConnectionId, roomCode, player.UserId, false, userName);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
                _logger.LogInformation($"Added {player.UserName} ({Context.ConnectionId}) to {roomCode}");

                // Send notifications
                await SendPlayerJoinNotifications(roomCode, gameSession, player, isReconnection);

                return PlayerJoinResult.Success(isReconnection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing player join for {userName} in room {roomCode}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Failed to join game");
                return PlayerJoinResult.Failed();
            }
        }

        /// <summary>
        /// Send current game state to reconnecting player
        /// </summary>
        private async Task SendReconnectionState(Player player, GameSession gameSession)
        {
            try
            {
                var reconnectionData = new ReconnectionStateResponse
                {
                    PlayerId = player.UserId,
                    UserName = player.UserName,
                    RoomCode = gameSession.RoomCode,
                    IsReconnecting = true,
                    GameState = gameSession.State.ToString(),
                    CurrentScore = player.Score,
                    CurrentRank = _playerManager.GetPlayerRank(gameSession.Players, player.UserId),
                    TotalPlayers = gameSession.Players.Count,

                    // Current question info if game is in progress
                    CurrentQuestionIndex = gameSession.CurrentQuestionIndex >= 0 ? gameSession.CurrentQuestionIndex + 1 : 0,
                    TotalQuestions = gameSession.Questions.Count,

                    // Current answers if player has answered current question
                    HasAnsweredCurrentQuestion = player.HasAnswered && player.CurrentQuestionIndex == gameSession.CurrentQuestionIndex,
                    CurrentAnswers = player.HasAnswered && player.CurrentQuestionIndex == gameSession.CurrentQuestionIndex ?
                        player.LastAnswerIds.ToList() : new List<string>(),

                    // Game timing info
                    IsWaitingForHost = gameSession.IsWaitingForHost,
                    QuestionStartTime = gameSession.QuestionStartTime,
                    QuestionEndTime = gameSession.QuestionEndTime,

                    // Player list
                    Players = _playerManager.CreatePlayerListData(gameSession.Players),

                    // Game background image and audio url from session
                    GameBackgroundBase64 = gameSession.GameBackgroundBase64,
                    GameAudioUrls = gameSession.GameAudioUrls
                };

                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.ReconnectState, reconnectionData);
                _logger.LogInformation($"Sent reconnection state to player {player.UserName} in room {gameSession.RoomCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending reconnection state to player {player.UserName}");
            }
        }

        /// <summary>
        /// Send player join notifications to all participants
        /// </summary>
        private async Task SendPlayerJoinNotifications(string roomCode, GameSession gameSession, Player player, bool isReconnection)
        {
            try
            {
                var playerListData = _playerManager.CreatePlayerListData(gameSession.Players);

                // Send to joining/reconnecting player
                var playerJoinedResponse = new PlayerJoinedResponse
                {
                    PlayerId = player.UserId,
                    UserName = player.UserName,
                    RoomCode = roomCode,
                    IsReconnecting = isReconnection,
                    GameState = gameSession.State.ToString(),
                    TotalPlayers = gameSession.Players.Count,
                    Players = playerListData,
                    CurrentScore = player.Score,
                    CurrentRank = _playerManager.GetPlayerRank(gameSession.Players, player.UserId),
                    GameBackgroundBase64 = gameSession.GameBackgroundBase64,
                    GameAudioUrls= gameSession.GameAudioUrls
                };
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.JoinedGame, playerJoinedResponse);

                // Send to all other players in the room
                var eventName = SignalREvents.PlayerJoined;
                var playerJoinedNotification = new PlayerJoinedNotificationResponse
                {
                    PlayerId = player.UserId,
                    UserName = player.UserName,
                    Score = player.Score,
                    TotalPlayers = gameSession.Players.Count,
                    IsReconnecting = isReconnection,
                    Players = playerListData
                };
                await _safeCommunication.SendToGroupSafe(roomCode, eventName, playerJoinedNotification);

                // Update host with lobby info
                await SendLobbyInfoToHost(roomCode, gameSession);

                _logger.LogInformation($"Sent join notifications for player {player.UserName} (reconnecting: {isReconnection}) to room {roomCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending join notifications for player {player.UserName}");
            }
        }

        /// <summary>
        /// Start game internal
        /// </summary>
        private async Task StartGameInternal(string roomCode, GameSession gameSession)
        {
            // Start the game session
            await _gameSessionService.StartGameSessionAsync(roomCode);

            // Update game state
            _gameSessionManager.UpdateGameState(roomCode, GameSessionState.InProgress);
            _gameSessionManager.ResetPlayersAnswerState(roomCode);

            // Send notifications
            var gameStartedResponse = new GameStartedResponse
            {
                TotalQuestions = gameSession.Questions.Count,
                TotalPlaytime = gameSession.Questions.Sum(q => q.TimeLimitSeconds)
            };
            await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.GameStarted, gameStartedResponse);

            // Schedule first question
            _safeCommunication.ExecuteInBackgroundWithDelay(
                () => SendNextQuestion(roomCode),
                TimeSpan.FromSeconds(3),
                $"FirstQuestion-{roomCode}");

            _logger.LogInformation($"Game started successfully in room {roomCode} with {gameSession.Players.Count} players");
        }

        /// <summary>
        /// Check if caller is host
        /// </summary>
        private bool IsCallerHost()
        {
            return _connectionManager.IsHost(Context.ConnectionId);
        }

        /// <summary>
        /// Send lobby information to host
        /// </summary>
        private async Task SendLobbyInfoToHost(string roomCode, GameSession gameSession)
        {
            var lobbyData = _playerManager.CreateLobbyInfoData(roomCode, gameSession);
            await _safeCommunication.SendToClientSafe(gameSession.HostConnectionId, SignalREvents.LobbyInfo, lobbyData);
        }

        /// <summary>
        /// Send the next question to all players in the game
        /// </summary>
        private async Task SendNextQuestion(string roomCode)
        {
            try
            {
                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    _logger.LogWarning($"Game session not found for room code: {roomCode}");
                    return;
                }

                // Increment question index
                gameSession!.CurrentQuestionIndex++;

                _logger.LogInformation($"Sending question {gameSession.CurrentQuestionIndex + 1} of {gameSession.Questions.Count} for room {roomCode}");

                // Check if we've reached the end of the questions
                if (gameSession.CurrentQuestionIndex >= gameSession.Questions.Count)
                {
                    _logger.LogInformation($"All questions completed for room {roomCode}. Ending game.");
                    await EndGameSafe(roomCode);
                    return;
                }

                // Get current question
                var question = gameSession.Questions[gameSession.CurrentQuestionIndex];
                _logger.LogInformation($"Current question: {question.Title} (ID: {question.Id})");

                // Get answers for this question
                if (question.Id != null)
                {
                    var answers = await _unitOfWork.Answers.GetAnswerByQuestionID(question.Id);

                    if (!answers.Any())
                    {
                        _logger.LogError($"No answers found for question {question.Id}. Skipping to next question.");
                        // Use background task to avoid Hub disposal issues
                        _safeCommunication.ExecuteInBackgroundWithDelay(
                            () => SendNextQuestion(roomCode),
                            TimeSpan.FromSeconds(2),
                            $"SkipQuestion-{roomCode}");
                        return;
                    }

                    _logger.LogInformation($"Found {answers.Count()} answers for question {question.Id}.");

                    // Reset all players' answered status for this question
                    _gameSessionManager.ResetPlayersAnswerState(roomCode);

                    // Set question timing
                    gameSession.QuestionStartTime = DateTime.UtcNow;
                    gameSession.QuestionEndTime = gameSession.QuestionStartTime.AddSeconds(question.TimeLimitSeconds);
                    gameSession.IsWaitingForHost = false;

                    // Prepare question data for players (without correct answers)
                    var questionData = new QuestionResponse
                    {
                        QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                        TotalQuestions = gameSession.Questions.Count,
                        QuestionId = question.Id ?? string.Empty,
                        QuestionText = question.Title,
                        QuestionType = question.Type.ToString(),
                        IsMultipleChoice = question.Type == QuestionType.MultipleChoice,
                        IsLastQuestion = gameSession.CurrentQuestionIndex >= gameSession.Questions.Count - 1,
                        TimeLimitSeconds = question.TimeLimitSeconds,
                        Answers = answers.Select(a => new AnswerOption { Id = a.Id ?? string.Empty, Title = a.Title }).ToList(),
                        StartTime = gameSession.QuestionStartTime,
                        BackgroundImageBase64 = gameSession.GameBackgroundBase64,
                        GameAudioUrls = gameSession.GameAudioUrls
                    };

                    // Prepare question data for host (with correct answers)
                    var hostQuestionData = new HostQuestionResponse
                    {
                        QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                        TotalQuestions = gameSession.Questions.Count,
                        QuestionId = question.Id ?? string.Empty,
                        QuestionText = question.Title,
                        QuestionType = question.Type.ToString(),
                        IsMultipleChoice = question.Type == QuestionType.MultipleChoice,
                        IsLastQuestion = gameSession.CurrentQuestionIndex >= gameSession.Questions.Count - 1,
                        TimeLimitSeconds = question.TimeLimitSeconds,
                        Answers = answers.Select(a => new HostAnswerOption { Id = a.Id ?? string.Empty, Title = a.Title, IsCorrect = a.IsCorrect }).ToList(),
                        CorrectAnswer = answers.Where(a => a.IsCorrect).Select(a => new HostAnswerOption { Id = a.Id ?? string.Empty, Title = a.Title, IsCorrect = a.IsCorrect }).FirstOrDefault(),
                        CorrectAnswers = answers.Where(a => a.IsCorrect).Select(a => new HostAnswerOption { Id = a.Id ?? string.Empty, Title = a.Title, IsCorrect = a.IsCorrect }).ToList(),
                        StartTime = gameSession.QuestionStartTime,
                        BackgroundImageBase64 = gameSession.GameBackgroundBase64,
                        GameAudioUrls = gameSession.GameAudioUrls
                    };

                    _logger.LogInformation($"Sending NewQuestion event to room {roomCode}");

                    // Send question to all players (without correct answers)
                    await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.NewQuestion, questionData);

                    // Send question to host (with correct answers visible)
                    _logger.LogInformation($"Sending HostNewQuestion event to host {gameSession.HostConnectionId}");
                    await _safeCommunication.SendToClientSafe(gameSession.HostConnectionId, SignalREvents.HostNewQuestion, hostQuestionData);
                    _logger.LogInformation($"HostNewQuestion event sent successfully to host");

                    _logger.LogInformation($"NewQuestion event sent successfully for room {roomCode}");

                    // Schedule automatic question completion check
                    var delay = TimeSpan.FromSeconds(question.TimeLimitSeconds + 2);
                    _logger.LogInformation($"Scheduling answer check in {delay.TotalSeconds} seconds for room {roomCode}");

                    // Use SafeCommunicationService to schedule background task
                    _safeCommunication.ExecuteInBackgroundWithDelay(
                        () => CheckQuestionCompletionSafe(roomCode),
                        delay,
                        $"QuestionCompletion-{roomCode}");
                }
                else
                {
                    _logger.LogError($"Question {question.Title} has null Id. Skipping.");
                    // Skip this question and move to next
                    _safeCommunication.ExecuteInBackgroundWithDelay(
                        () => SendNextQuestion(roomCode),
                        TimeSpan.FromSeconds(2),
                        $"SkipNullQuestion-{roomCode}");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendNextQuestion for room {roomCode}");

                // Try to notify players about the error and end game safely
                await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.Error, "An error occurred while loading the next question");
                await EndGameSafe(roomCode);
            }
        }

        /// <summary>
        /// Safely end a game session
        /// </summary>
        private async Task EndGameSafe(string roomCode)
        {
            try
            {
                await EndGame(roomCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending game safely for room {roomCode}");
            }
        }

        /// <summary>
        /// Check if question time has ended and prepare results for host review
        /// </summary>
        private async Task CheckQuestionCompletionSafe(string roomCode)
        {
            try
            {
                _logger.LogInformation($"CheckQuestionCompletionSafe called for room {roomCode}");

                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    _logger.LogWarning($"Game session not found in CheckQuestionCompletionSafe for room {roomCode}");
                    return;
                }

                _logger.LogInformation($"Processing question completion for room {roomCode}, question {gameSession!.CurrentQuestionIndex + 1}");

                var question = gameSession.Questions[gameSession.CurrentQuestionIndex];
                var answers = await _unitOfWork.Answers.GetAnswerByQuestionID(question.Id!);
                var correctAnswer = answers.FirstOrDefault(a => a.IsCorrect);
                var correctAnswers = answers.Where(a => a.IsCorrect).ToList();

                // Notify players that time is up
                var questionTimeEndedResponse = new QuestionTimeEndedResponse
                {
                    Message = "Time's up! Waiting for host to show results...",
                    QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                    TotalQuestions = gameSession.Questions.Count
                };
                await _safeCommunication.SendToGroupSafe(roomCode, SignalREvents.QuestionTimeEnded, questionTimeEndedResponse);

                // Calculate current leaderboard
                var leaderboard = _playerManager.CreateLeaderboard(gameSession.Players);

                // Check if this is the last question
                bool isLastQuestion = gameSession.CurrentQuestionIndex >= gameSession.Questions.Count - 1;

                _logger.LogInformation($"Question {gameSession.CurrentQuestionIndex + 1} of {gameSession.Questions.Count}, isLastQuestion: {isLastQuestion}");

                // Prepare detailed results for host
                var hostResultsData = new QuestionResultsResponse
                {
                    QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                    TotalQuestions = gameSession.Questions.Count,
                    QuestionText = question.Title,
                    QuestionType = question.Type.ToString(),
                    IsMultipleChoice = question.Type == QuestionType.MultipleChoice,
                    IsLastQuestion = isLastQuestion,
                    TimeLimitSeconds = question.TimeLimitSeconds,
                    Leaderboard = leaderboard,
                    PlayersAnswered = gameSession.Players.Count(p => p.HasAnswered),
                    TotalPlayers = gameSession.Players.Count,
                    HasMoreQuestions = !isLastQuestion,
                    AnswersWithStats = answers.Select(a => new AnswerStatistics
                    {
                        Id = a.Id ?? string.Empty,
                        Title = a.Title,
                        IsCorrect = a.IsCorrect,
                        PlayerCount = gameSession.Players.Count(p =>
                            p.LastAnswerIds.Contains(a.Id ?? "") &&
                            p.CurrentQuestionIndex == gameSession.CurrentQuestionIndex)
                    }).ToList()
                };

                // Send results to host for review
                await _safeCommunication.SendToClientSafe(gameSession.HostConnectionId, SignalREvents.QuestionResults, hostResultsData);

                // Send individual results to each player
                await SendPlayerResults(roomCode, gameSession, question, answers, correctAnswers, leaderboard, isLastQuestion);

                // Set game state to waiting for host
                _gameSessionManager.UpdateGameState(roomCode, GameSessionState.WaitingForHost);
                gameSession.IsWaitingForHost = true;

                _logger.LogInformation($"Question {gameSession.CurrentQuestionIndex + 1} completed for room {roomCode}. Waiting for host to proceed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking question completion for room {roomCode}");
            }
        }

        /// <summary>
        /// Send individual results to each player
        /// </summary>
        private async Task SendPlayerResults(string roomCode, GameSession gameSession, Question question,
            IEnumerable<Answer> answers, IEnumerable<Answer> correctAnswers, object leaderboard, bool isLastQuestion)
        {
            foreach (var player in gameSession.Players)
            {
                var playerRank = _playerManager.GetPlayerRank(gameSession.Players, player.UserId);

                // Check if player answered correctly based on question type
                var (answeredCorrect, correctnessRatio) = _playerManager.CalculatePlayerCorrectness(
                    player, question, correctAnswers, gameSession.CurrentQuestionIndex);

                var playerResultData = new PlayerQuestionResultResponse
                {
                    QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                    TotalQuestions = gameSession.Questions.Count,
                    QuestionText = question.Title,
                    QuestionType = question.Type.ToString(),
                    IsMultipleChoice = question.Type == QuestionType.MultipleChoice,
                    IsCorrect = answeredCorrect,
                    CurrentRank = playerRank,
                    Score = player.Score,
                    CorrectAnswer = correctAnswers.FirstOrDefault() != null ?
                        new CorrectAnswerInfo { Id = correctAnswers.First().Id, Title = correctAnswers.First().Title } : null,
                    CorrectAnswers = correctAnswers.Select(ca => new CorrectAnswerInfo { Id = ca.Id, Title = ca.Title }).ToList(),
                    Answers = answers.Select(a => new QuestionAnswerInfo
                    {
                        Id = a.Id,
                        Title = a.Title,
                        IsCorrect = a.IsCorrect,
                        PlayerCount = gameSession.Players.Count(p =>
                            p.LastAnswerIds.Contains(a.Id ?? "") &&
                            p.CurrentQuestionIndex == gameSession.CurrentQuestionIndex),
                        Selected = player.LastAnswerIds.Contains(a.Id ?? "")
                    }).ToList(),
                    TopPlayers = leaderboard,
                    PlayerAnswers = player.LastAnswerIds.ToList(),
                    IsLastQuestion = isLastQuestion,
                    CorrectnessRatio = correctnessRatio
                };

                await _safeCommunication.SendToClientSafe(player.ConnectionId, SignalREvents.PlayerQuestionResult, playerResultData);
            }
        }

        /// <summary>
        /// End the game and show final results
        /// </summary>
        private async Task EndGame(string roomCode)
        {
            try
            {
                if (!_gameSessionManager.TryGetGameSession(roomCode, out var gameSession))
                {
                    _logger.LogWarning($"Game session not found when ending game for room {roomCode}");
                    return;
                }

                _gameSessionManager.UpdateGameState(roomCode, GameSessionState.Completed);

                // Calculate final leaderboard
                var leaderboard = _playerManager.CreateLeaderboard(gameSession!.Players);

                // Prepare final results data
                var finalResultsData = new FinalResultsResponse
                {
                    Message = "Game completed!",
                    FinalLeaderboard = leaderboard,
                    TotalQuestions = gameSession.Questions.Count,
                    TotalPlayers = gameSession.Players.Count,
                    Winner = leaderboard.FirstOrDefault(),
                    TopThreePlayers = leaderboard.Take(3).ToList()
                };

                // Send to host with GameCompleted event
                await _safeCommunication.SendToClientSafe(gameSession.HostConnectionId, SignalREvents.GameCompleted, finalResultsData);

                // Send personalized results to each player
                foreach (var player in gameSession.Players)
                {
                    var playerRank = _playerManager.GetPlayerRank(gameSession.Players, player.UserId);

                    var personalizedData = new PersonalizedFinalResultsResponse
                    {
                        Message = "Game completed!",
                        FinalLeaderboard = leaderboard,
                        TotalQuestions = gameSession.Questions.Count,
                        TotalPlayers = gameSession.Players.Count,
                        Winner = leaderboard.FirstOrDefault(),
                        TopThreePlayers = leaderboard.Take(3).ToList(),
                        // Player-specific data
                        YourRank = playerRank,
                        YourScore = player.Score,
                        YourProgress = $"{player.CorrectAnswers}/{player.TotalAnswers}",
                        IsInTopThree = playerRank <= 3
                    };

                    // Send individual results to each player
                    await _safeCommunication.SendToClientSafe(player.ConnectionId, SignalREvents.FinalResults, personalizedData);

                    _safeCommunication.ExecuteInBackgroundWithDelay(
                        async () => await _safeCommunication.SendToClientSafe(player.ConnectionId, SignalREvents.GameEnded, SignalREvents.GameEnded),
                        TimeSpan.FromSeconds(3),
                        "GameEnd");
                }

                _logger.LogInformation($"Final results sent to all {gameSession.Players.Count} players individually");

                // Save the game session to the database and record completion analytics
                try
                {
                    await _gameSessionService.EndGameSessionAsync(roomCode);

                    // Update player statistics in database with hub session data before creating analytics
                    var dbGameSession = await _unitOfWork.GameSessions.GetByRoomCodeAsync(roomCode);
                    if (dbGameSession != null)
                    {
                        var allPlayers = await _unitOfWork.Players.GetByGameSessionIdAsync(dbGameSession.Id!);

                        // Use helper method to sync player data
                        await SyncPlayerDataToDatabase(gameSession.Players, allPlayers);

                        // Now create detailed session analytics with updated player data
                        await _analyticsService.CreateSessionAnalyticsAsync(dbGameSession, allPlayers);

                        _logger.LogInformation($"Analytics created successfully with updated player data for room {roomCode}");
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, $"Error saving final game session state or analytics for room {roomCode}");
                }

                // Clean up game session
                _gameSessionManager.RemoveGameSession(roomCode);

                var winner = leaderboard.FirstOrDefault();
                _logger.LogInformation($"Game ended for room {roomCode}. Winner: {winner?.GetType().GetProperty("UserName")?.GetValue(winner)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending game for room {roomCode}");
            }
        }

        /// <summary>
        /// Process answer submission based on question type
        /// </summary>
        private async Task<bool> ProcessAnswerSubmission(Player player, Question question, Answer answer, GameSession gameSession)
        {
            if (question.Type == QuestionType.MultipleChoice)
            {
                return await ProcessMultipleChoiceAnswer(player, answer, gameSession);
            }
            else
            {
                return await ProcessSingleChoiceAnswer(player, answer, gameSession);
            }
        }

        /// <summary>
        /// Process multiple choice answer submission
        /// </summary>
        private async Task<bool> ProcessMultipleChoiceAnswer(Player player, Answer answer, GameSession gameSession)
        {
            // For multiple choice - prevent deselection once answered
            if (player.HasAnswered && player.CurrentQuestionIndex == gameSession.CurrentQuestionIndex)
            {
                _logger.LogWarning($"Player {player.UserName} trying to select answer but already finalized MultipleChoice question {gameSession.CurrentQuestionIndex + 1}");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "You have already finalized your answers for this question");
                return false;
            }

            // Prevent deselection - once selected, answer cannot be removed
            if (player.LastAnswerIds.Contains(answer.Id!))
            {
                _logger.LogWarning($"Player {player.UserName} trying to deselect already selected answer {answer.Id} for MultipleChoice question - not allowed");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "Cannot deselect answers once they are chosen. You can only select additional answers.");
                return false;
            }

            // Add answer to player's selections
            player.LastAnswerIds.Add(answer.Id!);
            player.CurrentQuestionIndex = gameSession.CurrentQuestionIndex;

            // Track response time for MultipleChoice answers (each selection counts as a response)
            try
            {
                player.AddResponseTime(gameSession.QuestionStartTime, DateTime.UtcNow);
                _logger.LogInformation($"Player {player.UserName} MultipleChoice response time updated. New average: {player.AverageResponseTime:F2}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error tracking response time for player {player.UserName}");
            }

            _logger.LogInformation($"Player {player.UserName} added answer {answer.Id} to MultipleChoice selections. Total selected: {player.LastAnswerIds.Count}");
            return true;
        }

        /// <summary>
        /// Process single choice answer submission
        /// </summary>
        private async Task<bool> ProcessSingleChoiceAnswer(Player player, Answer answer, GameSession gameSession)
        {
            // For single choice - check if already answered
            if (player.HasAnswered && player.CurrentQuestionIndex == gameSession.CurrentQuestionIndex)
            {
                _logger.LogWarning($"Player {player.UserName} trying to change answer for SingleChoice question {gameSession.CurrentQuestionIndex + 1} - not allowed");
                await _safeCommunication.SendToCallerSafe(Clients.Caller, SignalREvents.Error, "You have already submitted your answer for this question");
                return false;
            }

            // Set answer and mark as answered
            player.LastAnswerIds.Clear();
            player.LastAnswerIds.Add(answer.Id!);
            player.HasAnswered = true;
            player.CurrentQuestionIndex = gameSession.CurrentQuestionIndex;

            // Track response time for SingleChoice answers
            try
            {
                player.AddResponseTime(gameSession.QuestionStartTime, DateTime.UtcNow);
                _logger.LogInformation($"Player {player.UserName} response time updated. New average: {player.AverageResponseTime:F2}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error tracking response time for player {player.UserName}");
            }

            // Calculate score for single choice
            await CalculateAndUpdatePlayerScore(player, gameSession);

            _logger.LogInformation($"Player {player.UserName} submitted SingleChoice answer {answer.Id} for question {gameSession.CurrentQuestionIndex + 1}");
            return true;
        }

        /// <summary>
        /// Calculate and update player score based on time-based scoring
        /// </summary>
        private async Task CalculateAndUpdatePlayerScore(Player player, GameSession gameSession)
        {
            var question = gameSession.Questions[gameSession.CurrentQuestionIndex];
            var answers = await _unitOfWork.Answers.GetAnswerByQuestionID(question.Id!);
            var correctAnswers = answers.Where(a => a.IsCorrect).ToList();

            var (answeredCorrect, correctnessRatio) = _playerManager.CalculatePlayerCorrectness(
                player, question, correctAnswers, gameSession.CurrentQuestionIndex);

            if (answeredCorrect)
            {
                // Calculate points based on response time
                var points = CalculatePointsBasedOnTime(
                    gameSession.QuestionStartTime, 
                    DateTime.UtcNow,
                    question.TimeLimitSeconds,
                    question.ScoreValue,                    
                    true);

                player.Score += points;
                player.CorrectAnswers++;
                
                _logger.LogInformation($"Player {player.UserName} scored {points} points for correct answer (response time based)");
            }
            else
            {
                _logger.LogInformation($"Player {player.UserName} scored 0 points for incorrect answer");
            }

            player.TotalAnswers++;
        }

        /// <summary>
        /// Calculate points based on response time - faster answers get more points
        /// </summary>
        private int CalculatePointsBasedOnTime(DateTime questionStartTime, DateTime answerTime, int questionTimeLimit, int questionScore, bool isCorrect)
        {
            if (!isCorrect)
            {
                return 0; // No points for incorrect answers
            }

            int maxPoints = questionScore; // Maximum points for instant answer
            int minPoints = 0;  // Minimum points for answer at time limit

            // Calculate response time in seconds, return double (e.g., 1.2, 5.4)
            var responseTime = (answerTime - questionStartTime).TotalSeconds; 
            
            // Ensure response time is within bounds
            responseTime = Math.Max(0, Math.Min(responseTime, questionTimeLimit));

            // Calculate points using linear decay from max to min
            // Formula: points = maxPoints - (responseTime / timeLimit) * (maxPoints - minPoints)
            var pointsDecay = (responseTime / questionTimeLimit) * (maxPoints - minPoints);
            var points = maxPoints - pointsDecay;

            // Round to nearest integer and ensure minimum points
            var finalPoints = Math.Max(minPoints, (int)Math.Round(points));

            _logger.LogInformation($"Points calculation: responseTime={responseTime:F2}s, timeLimit={questionTimeLimit}s, points={finalPoints}");
            
            return finalPoints;
        }

        /// <summary>
        /// Send progress update to host
        /// </summary>
        private async Task SendProgressUpdateToHost(string roomCode, GameSession gameSession)
        {
            var progressData = new ProgressUpdateResponse
            {
                QuestionIndex = gameSession.CurrentQuestionIndex + 1,
                TotalQuestions = gameSession.Questions.Count,
                PlayersAnswered = gameSession.Players.Count(p => p.HasAnswered),
                TotalPlayers = gameSession.Players.Count,
                Players = gameSession.Players.Select(p => new PlayerProgressInfo
                {
                    UserId = p.UserId,
                    UserName = p.UserName,
                    HasAnswered = p.HasAnswered,
                    Score = p.Score
                }).ToList()
            };

            await _safeCommunication.SendToClientSafe(gameSession.HostConnectionId, SignalREvents.LobbyInfo, progressData);
        }

        /// <summary>
        /// Helper method to sync Hub player data with Database player data
        /// </summary>
        private async Task SyncPlayerDataToDatabase(IEnumerable<Player> hubPlayers, IEnumerable<Player> dbPlayers)
        {
            var hubPlayersList = hubPlayers.ToList();
            var dbPlayersList = dbPlayers.ToList();

            _logger.LogInformation($"Starting player data sync - Hub players: {hubPlayersList.Count}, DB players: {dbPlayersList.Count}");
            _logger.LogInformation($"Hub players IDs: {string.Join(", ", hubPlayersList.Select(p => $"{p.UserName}:{p.UserId}"))}");
            _logger.LogInformation($"DB players IDs: {string.Join(", ", dbPlayersList.Select(p => $"{p.UserName}:{p.Id}"))}");

            foreach (var dbPlayer in dbPlayersList)
            {
                var hubPlayer = hubPlayersList.FirstOrDefault(p => p.UserId == dbPlayer.Id);
                if (hubPlayer != null)
                {
                    _logger.LogInformation($"Syncing {hubPlayer.UserName}: Hub(Score={hubPlayer.Score}, Correct={hubPlayer.CorrectAnswers}/{hubPlayer.TotalAnswers}) -> DB(Score={dbPlayer.Score}, Correct={dbPlayer.CorrectAnswers}/{dbPlayer.TotalAnswers})");

                    // Sync all important statistics from hub player to database player
                    dbPlayer.Score = hubPlayer.Score;
                    dbPlayer.CorrectAnswers = hubPlayer.CorrectAnswers;
                    dbPlayer.TotalAnswers = hubPlayer.TotalAnswers;
                    dbPlayer.LastAnsweredAt = hubPlayer.LastAnsweredAt ?? DateTime.UtcNow;
                    dbPlayer.IsConnected = hubPlayer.IsConnected;
                    dbPlayer.AverageResponseTime = hubPlayer.AverageResponseTime; // Sync response time data

                    // Update the player in database
                    await _unitOfWork.Players.UpdateAsync(dbPlayer);
                    _logger.LogInformation($"Synced player {dbPlayer.UserName} data: Score={dbPlayer.Score}, Correct={dbPlayer.CorrectAnswers}/{dbPlayer.TotalAnswers}, AvgTime={dbPlayer.AverageResponseTime:F2}s");
                }
                else
                {
                    _logger.LogWarning($"Could not find Hub player for DB player: {dbPlayer.UserName} (DB ID: {dbPlayer.Id}, DB UserId: {dbPlayer.UserId})");
                    _logger.LogWarning($"Available Hub players: {string.Join(", ", hubPlayersList.Select(p => $"{p.UserName}(UserId:{p.UserId})"))}");
                }
            }

            _logger.LogInformation("All player data synced to database successfully");
        }

        #endregion
    }
}