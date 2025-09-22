using AutoMapper;
using BDKahoot.Application.Games.Commands.CreateGame;
using BDKahoot.Application.Games.Commands.DeleteGame;
using BDKahoot.Application.Games.Commands.UpdateGame;
using BDKahoot.Application.Games.Queries.GetAllGames;
using BDKahoot.Application.Games.Queries.GetGameById;
using BDKahoot.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.BDKahoot.Application.Games
{
    public class GameHandlerTestFixture
    {
        // Shared mocks
        public Mock<IMapper> MapperMock { get; } = new();
        public Mock<IUnitOfWork> UnitOfWorkMock { get; } = new();
        public Mock<IGameRepository> GameRepositoryMock { get; } = new();

        private readonly Dictionary<Type, object> _loggerMocks = new();

        public Mock<ILogger<THandler>> GetLoggerMock<THandler>() where THandler : class
        {
            if (!_loggerMocks.TryGetValue(typeof(THandler), out var mock))
            {
                mock = new Mock<ILogger<THandler>>();
                _loggerMocks[typeof(THandler)] = mock;
            }
            return (Mock<ILogger<THandler>>)mock;
        }

        public GameHandlerTestFixture()
        {
            UnitOfWorkMock.Setup(u => u.Games).Returns(GameRepositoryMock.Object);
        }

        // Factories
        public CreateGameCommandHandler InitializeCreateHandler() =>
            new(GetLoggerMock<CreateGameCommandHandler>().Object, MapperMock.Object, UnitOfWorkMock.Object);

        public UpdateGameCommandHandler InitializeUpdateHandler() =>
            new(GetLoggerMock<UpdateGameCommandHandler>().Object, MapperMock.Object, UnitOfWorkMock.Object);

        public DeleteGameCommandHandler InitializeDeleteHandler() =>
            new(GetLoggerMock<DeleteGameCommandHandler>().Object, UnitOfWorkMock.Object);

        public GetAllGamesQueryHandler InitializeGetAllHandler() =>
            new(GetLoggerMock<GetAllGamesQueryHandler>().Object, MapperMock.Object, UnitOfWorkMock.Object);

        public GetGameByIdQueryHandler InitializeGetByIdHandler() =>
            new(GetLoggerMock<GetGameByIdQueryHandler>().Object, MapperMock.Object, UnitOfWorkMock.Object);
    }
}
