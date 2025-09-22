using BDKahoot.Application.Games.Commands.CreateGame;
using BDKahoot.Application.Games.Commands.UpdateGame;
using BDKahoot.Domain.Models;
using BDKahoot.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace BDKahoot.IntegrationTests.BDKahoot.API
{
    public class GamesControllerIntegrationTests : IClassFixture<WebApplicationFactoryTest>, IDisposable
    {
        private readonly HttpClient _client;

        public GamesControllerIntegrationTests(WebApplicationFactoryTest factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateGame_Then_GetGameById_ReturnsCreatedAndGame()
        {
            // Arrange
            var createCommand = new CreateGameCommand
            {
                Title = "IntegrationTestGame",
                Description = "Test Desc",
                UserNTID = "tut3hc@bosch.com"
            };

            // Act
            var createResponse = await _client.PostAsJsonAsync("/api/games", createCommand);

            // Assert
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var location = createResponse.Headers.Location?.ToString();
            location.Should().NotBeNull();

            // Extract id from location
            var id = location!.Split('/').Last();

            // Act: Get by id
            var getResponse = await _client.GetAsync($"/api/games/{id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var game = await getResponse.Content.ReadFromJsonAsync<Game>();
            game.Should().NotBeNull();
            game!.Title.Should().Be("IntegrationTestGame");
            game.HostUserNTID.Should().Be("tut3hc@bosch.com");
        }

        [Fact]
        public async Task GetGames_ReturnsList()
        {
            // Act
            var response = await _client.GetAsync("/api/games");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var games = await response.Content.ReadFromJsonAsync<List<Game>>();
            games.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateGame_Then_GetGameById_ReturnsUpdated()
        {
            // Arrange: Create game
            var createCommand = new CreateGameCommand
            {
                Title = "GameToUpdate",
                Description = "Desc",
                UserNTID = "host-2"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/games", createCommand);
            var id = createResponse.Headers.Location?.ToString()!.Split('/').Last();

            // Act: Update
            var updateCommand = new UpdateGameCommand
            {
                Title = "GameUpdated",
                Description = "Updated Desc",
                UserNTID = "host-2"
            };
            var updateResponse = await _client.PatchAsJsonAsync($"/api/games/{id}", updateCommand);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act: Get by id
            var getResponse = await _client.GetAsync($"/api/games/{id}");
            var game = await getResponse.Content.ReadFromJsonAsync<Game>();
            game!.Title.Should().Be("GameUpdated");
            game.Description.Should().Be("Updated Desc");
        }

        [Fact]
        public async Task DeleteGame_Then_GetGameById_ReturnsNotFound()
        {
            // Arrange: Create game
            var createCommand = new CreateGameCommand
            {
                Title = "GameToDelete",
                Description = "Desc",
                UserNTID = "host-3"
            };
            var createResponse = await _client.PostAsJsonAsync("/api/games", createCommand);
            var id = createResponse.Headers.Location?.ToString()!.Split('/').Last();

            // Act: Delete
            var deleteResponse = await _client.DeleteAsync($"/api/games/{id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act: Get by id
            var getResponse = await _client.GetAsync($"/api/games/{id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
