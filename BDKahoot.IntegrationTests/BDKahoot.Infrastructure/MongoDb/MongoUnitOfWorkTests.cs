using BDKahoot.Domain.Models;
using BDKahoot.Infrastructure.MongoDb;

namespace BDKahoot.IntegrationTests.BDKahoot.Infrastructure.MongoDb
{
    // If you're using IClassFixture<T>, let xUnit handle Dispose() automatically.
    public class MongoUnitOfWorkTests : IClassFixture<MongoDbFixture>
    {
        private readonly MongoDbFixture _mongoDbFixture;
        private readonly MongoUnitOfWork _mongoUnitOfWork;

        public MongoUnitOfWorkTests(MongoDbFixture mongoDbFixture)
        {
            _mongoDbFixture = mongoDbFixture;
            _mongoUnitOfWork = new MongoUnitOfWork(_mongoDbFixture.GetDbContext());
        }

        [Theory]
        [Trait("Category", "IntegrationTest_Game_AddAsync")]
        [InlineData("UnitOfWork_IntegrationTestGame1", "This is the description 1", "host-1", true)]
        [InlineData("UnitOfWork_IntegrationTestGame2", "This is the description 2", "host-2", true)]
        [InlineData("UnitOfWork_IntegrationTestGame3", "This is the description 3", "host-3", true)]
        public async Task GameAddAsync_ShouldReturn_Game(string title, string description, string hostUserId, bool expecteFoundInDB)
        {
            // Arrange
            var game = new Game
            {
                Title = title,
                Description = description,
                HostUserNTID = hostUserId
            };

            // Act
            await _mongoUnitOfWork.Games.AddAsync(game);
            bool gameCreated = await _mongoUnitOfWork.Games.AnyAsync(g => g.Title == title);

            // Assert
            Assert.Equal(expecteFoundInDB, gameCreated);
        }

        [Theory]
        [Trait("Category", "IntegrationTest_User_AddAsync")]
        [InlineData("testuser@domain.com", "testuser@domain.com", "firstname", "lastname", true)]
        public async Task UserAddAsync_ShouldReturn_User(string upn, string emailAddress, string firstName, string lastName, bool expecteFoundInDB)
        {
            // Arrange
            var user = new User
            {
                Upn = upn,
                EmailAddress = emailAddress,
                FirstName = firstName,
                Lastname = lastName
            };

            // Act
            await _mongoUnitOfWork.Users.AddAsync(user);
            bool userCreated = await _mongoUnitOfWork.Users.AnyAsync(u => u.Upn == upn);

            // Assert
            Assert.Equal(expecteFoundInDB, userCreated);
        }
    }
}
