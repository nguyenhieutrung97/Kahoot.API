using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Models.Players
{
    /// <summary>
    /// Result of player validation during join operations
    /// </summary>
    public class PlayerValidationResult
    {
        public bool IsReconnection { get; set; }
        public Player? ExistingPlayer { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}
