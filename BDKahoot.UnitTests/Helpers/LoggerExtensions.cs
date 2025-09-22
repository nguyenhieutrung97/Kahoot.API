using Microsoft.Extensions.Logging;
using Moq;

namespace BDKahoot.UnitTests.Helpers
{
    public static class LoggerExtensions
    {
        public static void VerifyLogContains<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string containsMessage, Times? times = null)
        {
            loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
