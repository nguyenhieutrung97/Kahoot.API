using BDKahoot.Domain.Models;
using BDKahoot.Infrastructure.MongoDb;
using BDKahoot.Infrastructure.Repositories;
using FluentAssertions;

namespace BDKahoot.IntegrationTests.BDKahoot.Infrastructure.Repositories
{
    // If you're not using IClassFixture<T>, it's safe to manage the lifecycle and call Dispose() manually.
    public class UserRepositoryIntegrationTests : IDisposable
    {
        private readonly MongoDbFixture mongo2GoFixture = new();
        private readonly MongoDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryIntegrationTests()
        {
            _context = mongo2GoFixture.GetDbContext();
            _repository = new UserRepository(_context);
        }

        [Fact]
        [Trait("Category", "IntegrationTest_User_AddAsync")]
        public async Task AddAndGetByUpnAsync_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Upn = "testuser@domain.com",
                EmailAddress = "testuser@domain.com",
                FirstName = "Test",
                Lastname = "User"
            };

            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByUpnAsync("testuser@domain.com");

            // Assert
            result.Should().NotBeNull();
            result!.Upn.Should().Be("testuser@domain.com");
            result.EmailAddress.Should().Be("testuser@domain.com");
            result.FirstName.Should().Be("Test");
            result.Lastname.Should().Be("User");
        }

        public void Dispose()
        {
            mongo2GoFixture.Dispose();
        }
    }
}
