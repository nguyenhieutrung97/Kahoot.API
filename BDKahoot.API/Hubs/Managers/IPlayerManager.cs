using BDKahoot.API.Hubs.Models.Players;
using BDKahoot.API.Hubs.Models.Game;
using BDKahoot.Domain.Models;

namespace BDKahoot.API.Hubs.Managers
{
    public interface IPlayerManager
    {
        Task<PlayerValidationResult> ValidatePlayerJoin(GameSession gameSession, string userName, string connectionId, string? playerId);
        Task<Player?> CreateNewPlayer(string roomCode, string userName, string connectionId);
        Player CreateHubPlayerFromDb(Domain.Models.Player dbPlayer, string connectionId);
        Task<bool> ReconnectPlayer(string roomCode, string playerId, string connectionId);
        void UpdatePlayerConnectionInfo(Player player, string newConnectionId);
        void MarkPlayerAsDisconnected(Player player);
        Task<bool> RemovePlayerFromSession(string roomCode, string playerId);
        List<PlayerListItem> CreatePlayerListData(List<Player> players);
        LobbyInfoData CreateLobbyInfoData(string roomCode, GameSession gameSession);
        List<LeaderboardEntry> CreateLeaderboard(IEnumerable<Player> players);
        int GetPlayerRank(IEnumerable<Player> players, string playerId);
        (bool answeredCorrect, double correctnessRatio) CalculatePlayerCorrectness(
            Player player, Question question, IEnumerable<Answer> correctAnswers, int currentQuestionIndex);
    }
}
