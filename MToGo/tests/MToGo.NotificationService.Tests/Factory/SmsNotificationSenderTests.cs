using LegacyMToGo.Services.Notifications;
using Microsoft.Extensions.Logging;
using Moq;

namespace MToGo.NotificationService.Tests.Factory;

/// <summary>
/// Tests for the SMS Notification Sender.
/// </summary>
public class SmsNotificationSenderTests
{
    private readonly Mock<ILogger<SmsNotificationSender>> _mockLogger;
    private readonly SmsNotificationSender _target;

    public SmsNotificationSenderTests()
    {
        _mockLogger = new Mock<ILogger<SmsNotificationSender>>();
        _target = new SmsNotificationSender(_mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidInput_CompletesSuccessfully()
    {
        // Arrange
        var destination = "+4512345678";
        var message = "Your order has been confirmed!";

        // Act
        await _target.SendAsync(destination, message);

        // Assert - verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("+4512345678", "Hello World")]
    [InlineData("+4587654321", "Order #123 shipped")]
    [InlineData("+1234567890", "New notification")]
    public async Task SendAsync_WithVariousPhoneNumbers_LogsCorrectly(string destination, string message)
    {
        // Act
        await _target.SendAsync(destination, message);

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
        var destination = "+4512345678";

        // Act
        await _target.SendAsync(destination, message);

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
        var destination = "+4512345678";
        string? message = null;

        // Act & Assert - Should not throw
        await _target.SendAsync(destination, message!);
    }

    [Fact]
    public async Task SendAsync_ImplementsINotificationSender()
    {
        // Assert - Verify the type implements the interface
        Assert.IsAssignableFrom<INotificationSender>(_target);
        
        // Act & Assert - Should be callable through interface
        INotificationSender sender = _target;
        await sender.SendAsync("+4512345678", "message");
    }
}

