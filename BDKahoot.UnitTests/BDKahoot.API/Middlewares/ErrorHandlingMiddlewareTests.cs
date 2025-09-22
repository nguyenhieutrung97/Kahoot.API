using BDKahoot.API.Middlewares;
using BDKahoot.Domain.Exceptions;
using BDKahoot.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace BDKahoot.UnitTests.BDKahoot.API.Middlewares
{
    public class ErrorHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock;
        private readonly ErrorHandlingMiddleware _middleware;
        private readonly Mock<RequestDelegate> _nextMock;

        public ErrorHandlingMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
            _middleware = new ErrorHandlingMiddleware(_loggerMock.Object);
            _nextMock = new Mock<RequestDelegate>();
        }

        [Fact]
        public async Task InvokeAsync_WithNoException_CallsNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _nextMock.Setup(n => n(context)).Returns(Task.CompletedTask);

            // Act
            await _middleware.InvokeAsync(context, _nextMock.Object);

            // Assert
            _nextMock.Verify(n => n(context), Times.Once);
            context.Response.StatusCode.Should().Be(200); // Default
        }

        [Fact]
        public async Task InvokeAsync_WithNotFoundExceptionCustom_Returns404()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            
            var exception = new NotFoundExceptionCustom("Game", "123");
            _nextMock.Setup(n => n(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context, _nextMock.Object);

            // Assert
            context.Response.StatusCode.Should().Be(404);
            context.Response.ContentType.Should().Be("text/plain");

            // Verify warning was logged
            _loggerMock.VerifyLogContains(
                LogLevel.Warning,
                "Game with id: 123 doesn't exist.");
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedAccessExceptionCustom_Returns401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            
            var exception = new UnauthorizedAccessExceptionCustom("Unauthorized access");
            _nextMock.Setup(n => n(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context, _nextMock.Object);

            // Assert
            context.Response.StatusCode.Should().Be(401);
            context.Response.ContentType.Should().Be("text/plain");

            // Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unauthorized access")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidMongoDbIdException_Returns400WithCustomMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            
            var exception = new Exception("Value 'invalidId' is not a valid 24 digit hex string.");
            _nextMock.Setup(n => n(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context, _nextMock.Object);

            // Assert
            context.Response.StatusCode.Should().Be(400);
            context.Response.ContentType.Should().Be("text/plain");

            // Read response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            responseBody.Should().Be("Invalid ID format. Must be a 24-character hexadecimal string.");

            // Verify error was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is not a valid 24 digit hex string")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithUnsupportedMappingException_Returns500WithCustomMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            
            var exception = new Exception("Missing type map configuration or unsupported mapping error");
            _nextMock.Setup(n => n(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context, _nextMock.Object);

            // Assert
            context.Response.StatusCode.Should().Be(500);
            context.Response.ContentType.Should().Be("text/plain");

            // Read response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            responseBody.Should().Be("Mapping configuration error. Please check your AutoMapper setup.");

            // Verify error was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Missing type map configuration or unsupported mapping")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithGenericException_Returns500WithGenericMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            
            var exception = new Exception("Some generic error");
            _nextMock.Setup(n => n(context)).ThrowsAsync(exception);

            // Act
            await _middleware.InvokeAsync(context, _nextMock.Object);

            // Assert
            context.Response.StatusCode.Should().Be(500);
            context.Response.ContentType.Should().Be("text/plain");

            // Read response body
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            responseBody.Should().Be("Something went wrong!");

            // Verify error was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Some generic error")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
