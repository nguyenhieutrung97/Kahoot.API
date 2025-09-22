using BDKahoot.Domain.Models;
using BDKahoot.Domain.Enums;
using BDKahoot.API.Hubs.Models.Validation;

namespace BDKahoot.API.Hubs.Services
{
    /// <summary>
    /// Provides validation logic for game operations
    /// </summary>
    public class GameValidationService : IGameValidationService
    {
        /// <summary>
        /// Validate if a user can create a game room
        /// </summary>
        public ValidationResult ValidateGameRoomCreation(string? userId, string? userNTID, Game? game)
        {
            return ValidationResult.Success();
            if (string.IsNullOrEmpty(userId))
            {
                return ValidationResult.Failure("Authentication required to create a game room");
            }

            if (game == null)
            {
                return ValidationResult.Failure("Game not found");
            }

            if (game.HostUserNTID != userNTID)
            {
                return ValidationResult.Failure("Unauthorized! You're not the owner of the game.");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate if a game can be started
        /// </summary>
        public ValidationResult ValidateGameStart(GameSession gameSession, bool isHost)
        {
            return ValidationResult.Success();
            if (!isHost)
            {
                return ValidationResult.Failure("Only the host can start the game");
            }

            if (gameSession.Players.Count < 2)
            {
                return ValidationResult.Failure("Need at least two players to start the game");
            }

            if (gameSession.Questions == null || gameSession.Questions.Count == 0)
            {
                return ValidationResult.Failure("No questions available for this game");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate player join request
        /// </summary>
        public ValidationResult ValidatePlayerJoin(string userName, GameSession gameSession, bool isReconnection = false)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return ValidationResult.Failure("Username cannot be empty");
            }

            // Allow reconnection for existing players regardless of game state
            if (isReconnection)
            {
                return ValidationResult.Success();
            }

            // For new players, only allow join in lobby state
            if (gameSession.State != GameSessionState.Lobby)
            {
                return ValidationResult.Failure($"Cannot join game - Game is currently {gameSession.State}. Only existing players can reconnect.");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate player kick operation
        /// </summary>
        public ValidationResult ValidatePlayerKick(GameSession gameSession, bool isHost)
        {
            return ValidationResult.Success();
            if (!isHost)
            {
                return ValidationResult.Failure("Only the host can kick players");
            }

            if (gameSession.State != GameSessionState.Lobby)
            {
                return ValidationResult.Failure("Can only kick players in lobby");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validate answer submission
        /// </summary>
        public ValidationResult ValidateAnswerSubmission(Player player, GameSession gameSession)
        {
            if (gameSession.State != GameSessionState.InProgress)
            {
                return ValidationResult.Failure("Game is not in progress");
            }

            if (player.HasAnswered && gameSession.Questions[gameSession.CurrentQuestionIndex].Type != QuestionType.MultipleChoice)
            {
                return ValidationResult.Failure("You have already answered this question");
            }

            return ValidationResult.Success();
        }
    }
}
