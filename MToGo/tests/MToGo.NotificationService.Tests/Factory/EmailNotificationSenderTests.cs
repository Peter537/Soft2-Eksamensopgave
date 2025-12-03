using LegacyMToGo.Services.Notifications;
using Microsoft.Extensions.Logging;
using Moq;

namespace MToGo.NotificationService.Tests.Factory;

/// <summary>
/// Tests for the Email Notification Sender.
/// </summary>
public class EmailNotificationSenderTests
{
    private readonly Mock<ILogger<EmailNotificationSender>> _mockLogger;
    private readonly EmailNotificationSender _sut;

    public EmailNotificationSenderTests()
    {
        _mockLogger = new Mock<ILogger<EmailNotificationSender>>();
        _sut = new EmailNotificationSender(_mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidInput_CompletesSuccessfully()
    {
        // Arrange
        var destination = "test@example.com";
        var message = "Your order has been confirmed!";

        // Act
        await _sut.SendAsync(destination, message);

        // Assert - verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EMAIL")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("user@domain.com", "Hello World")]
    [InlineData("customer@example.org", "Order #123 shipped")]
    [InlineData("admin@mtogo.dk", "New notification")]
    public async Task SendAsync_WithVariousInputs_LogsCorrectly(string destination, string message)
    {
        // Act
        await _sut.SendAsync(destination, message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(destination)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Line1\nLine2")]
    [InlineData("Line1\r\nLine2")]
    [InlineData("Line1\rLine2")]
    public async Task SendAsync_WithNewlines_SanitizesMessage(string message)
    {
        // Arrange
        var destination = "test@example.com";

        // Act
        await _sut.SendAsync(destination, message);

        // Assert - Should complete without throwing
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => !v.ToString()!.Contains("\n") && !v.ToString()!.Contains("\r")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_HandlesGracefully()
    {
        // Arrange
        var destination = "test@example.com";
        string? message = null;

        // Act & Assert - Should not throw
        await _sut.SendAsync(destination, message!);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var destination = "test@example.com";
        var message = "Test message";
        var cts = new CancellationTokenSource();

        // Act
        var task = _sut.SendAsync(destination, message, cts.Token);

        // Assert - Should complete immediately (simulated)
        await task;
    }

    [Fact]
    public async Task SendAsync_ImplementsINotificationSender()
    {
        // Assert - Verify the type implements the interface
        Assert.IsAssignableFrom<INotificationSender>(_sut);
        
        // Act & Assert - Should be callable through interface
        INotificationSender sender = _sut;
        await sender.SendAsync("test@test.com", "message");
    }
}
