using BDKahoot.Application.Services.Models;
using BDKahoot.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Application.Services.GameSessionService
{
    public interface IGameSessionService
    {
        Task<GameSession> CreateGameSessionAsync(string gameId, string hostConnectionId);
        Task<GameSession?> GetActiveSessionAsync(string roomCode);
        Task<Player> AddPlayerToSessionAsync(string roomCode, string userName, string connectionId);
        Task RemovePlayerFromSessionAsync(string roomCode, string userId);
        Task<Player> ReconnectPlayerAsync(string roomCode, string playerId, string newConnectionId);
        Task StartGameSessionAsync(string roomCode);
        Task EndGameSessionAsync(string roomCode, bool hostDisconnected = false);
        Task<IEnumerable<Player>> GetSessionLeaderboardAsync(string roomCode);
        Task<SessionStatistics> GetSessionStatisticsAsync(string roomCode);
    }
}
