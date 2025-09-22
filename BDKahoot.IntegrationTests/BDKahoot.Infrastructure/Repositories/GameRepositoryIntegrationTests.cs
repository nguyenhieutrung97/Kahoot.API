using BDKahoot.Domain.Models;
using BDKahoot.Infrastructure.Repositories;
using FluentAssertions;
using MongoDB.Driver;

namespace BDKahoot.IntegrationTests.BDKahoot.Infrastructure.Repositories
{
    // If you're not using IClassFixture<T>, it's safe to manage the lifecycle and call Dispose() manually.
    public class GameRepositoryIntegrationTests : IDisposable
    {
        private readonly MongoDbFixture mongo2GoFixture = new();
        private readonly GameRepository _repository;

        public GameRepositoryIntegrationTests()
        {
            _repository = new GameRepository(mongo2GoFixture.GetDbContext());
        }

        [Theory]
        [Trait("Category", "IntegrationTest_Game_AddAsync")]
        [InlineData("IntegrationTestGame1", "This is the description 1", "host-1")]
        [InlineData("IntegrationTestGame2", "This is the description 2", "host-2")]
        [InlineData("IntegrationTestGame3", "This is the description 3", "host-3")]
        public async Task AddAndGetByTitleAsync_ReturnsGame(string title, string description, string hostUserId)
        {
            // Arrange
            var game = new Game
            {
                Title = title,
                Description = description,
                HostUserNTID = hostUserId
            };

            await _repository.AddAsync(game);

            // Act
            var result = await _repository.GetByTitleAsync(title);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be(title);
            result.HostUserNTID.Should().Be(hostUserId);
        }

        [Fact]
        public async Task GetGamesByHostUserIdAsync_ReturnsCorrectGames()
        {
            // Arrange
            var game1 = new Game { Title = "Game1", Description = "Desc1", HostUserNTID = "host-2" };
            var game2 = new Game { Title = "Game2", Description = "Desc2", HostUserNTID = "host-2" };
            var game3 = new Game { Title = "Game3", Description = "Desc3", HostUserNTID = "host-3" };

            await _repository.AddAsync(game1);
            await _repository.AddAsync(game2);
            await _repository.AddAsync(game3);

            // Act
            var result = (await _repository.GetGamesByHostUserNTIDAsync("host-2")).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(g => g.HostUserNTID == "host-2");
        }

        public void Dispose()
        {
            mongo2GoFixture.Dispose();
        }
    }
}
