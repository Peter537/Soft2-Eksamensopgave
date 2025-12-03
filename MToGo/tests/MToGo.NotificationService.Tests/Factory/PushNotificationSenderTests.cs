using LegacyMToGo.Services.Notifications;
using Microsoft.Extensions.Logging;
using Moq;

namespace MToGo.NotificationService.Tests.Factory;

/// <summary>
/// Tests for the Push Notification Sender.
/// </summary>
public class PushNotificationSenderTests
{
    private readonly Mock<ILogger<PushNotificationSender>> _mockLogger;
    private readonly PushNotificationSender _sut;

    public PushNotificationSenderTests()
    {
        _mockLogger = new Mock<ILogger<PushNotificationSender>>();
        _sut = new PushNotificationSender(_mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidInput_CompletesSuccessfully()
    {
        // Arrange
        var destination = "device-token-abc123";
        var message = "Your order has been confirmed!";

        // Act
        await _sut.SendAsync(destination, message);

        // Assert - verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PUSH")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("device-token-123", "Hello World")]
    [InlineData("firebase-token-xyz", "Order #123 shipped")]
    [InlineData("apns-device-token", "New notification")]
    public async Task SendAsync_WithVariousDeviceTokens_LogsCorrectly(string destination, string message)
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
        var destination = "device-token-123";

        // Act
        await _sut.SendAsync(destination, message);

        // Assert - Should complete without throwing and sanitize newlines
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
        var destination = "device-token-123";
        string? message = null;

        // Act & Assert - Should not throw
        await _sut.SendAsync(destination, message!);
    }

    [Fact]
    public async Task SendAsync_ImplementsINotificationSender()
    {
        // Assert - Verify the type implements the interface
        Assert.IsAssignableFrom<INotificationSender>(_sut);
        
        // Act & Assert - Should be callable through interface
        INotificationSender sender = _sut;
        await sender.SendAsync("device-token", "message");
    }
}
