using BDKahoot.Domain.Models;
using BDKahoot.Domain.Repositories;
using BDKahoot.Infrastructure.MongoDb;
using MongoDB.Driver;

namespace BDKahoot.Infrastructure.Repositories
{
    public class GameSessionRepository : GenericRepository<GameSession>, IGameSessionRepository
    {
        public GameSessionRepository(MongoDbContext context) : base(context, "GameSessions")
        {
        }

        public async Task<GameSession?> GetByRoomCodeAsync(string roomCode)
        {
            return await Collection.Find(x => x.RoomCode == roomCode && !x.Deleted).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<GameSession>> GetByGameIdAsync(string gameId)
        {
            return await Collection.Find(x => x.GameId == gameId && !x.Deleted).ToListAsync();
        }

        public async Task<IEnumerable<GameSession>> GetActiveSessionsAsync()
        {
            return await Collection.Find(x => (x.State == Domain.Enums.GameSessionState.Lobby || 
                                             x.State == Domain.Enums.GameSessionState.InProgress ||
                                             x.State == Domain.Enums.GameSessionState.WaitingForHost) && !x.Deleted)
                                   .ToListAsync();
        }

        public async Task<bool> IsRoomCodeUniqueAsync(string roomCode)
        {
            var existingSession = await Collection.Find(x => x.RoomCode == roomCode && !x.Deleted).FirstOrDefaultAsync();
            return existingSession == null;
        }

        public async Task<GameSession?> GetSessionWithPlayersAsync(string roomCode)
        {
            // Note: In MongoDB, we'll need to handle Players separately as they're runtime data
            // This method will get the session and then populate players from memory/cache
            return await GetByRoomCodeAsync(roomCode);
        }
    }
}
