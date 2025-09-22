using BDKahoot.Domain.Models;
using BDKahoot.API.Hubs.Models.Validation;

namespace BDKahoot.API.Hubs.Services
{
    /// <summary>
    /// Interface for validation logic for game operations
    /// </summary>
    public interface IGameValidationService
    {
        ValidationResult ValidateGameRoomCreation(string? userId, string? userNTID, Game? game);
        ValidationResult ValidateGameStart(GameSession gameSession, bool isHost);
        ValidationResult ValidatePlayerJoin(string userName, GameSession gameSession, bool isReconnection = false);
        ValidationResult ValidatePlayerKick(GameSession gameSession, bool isHost);
        ValidationResult ValidateAnswerSubmission(Player player, GameSession gameSession);
    }
}
