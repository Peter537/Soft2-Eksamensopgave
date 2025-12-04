using LegacyMToGo.Models;
using LegacyMToGo.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MToGo.NotificationService.Tests.Factory;

/// <summary>
/// Tests for the Notification Sender Factory (Factory Method pattern).
/// </summary>
public class NotificationSenderFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly NotificationSenderFactory _sut;

    public NotificationSenderFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _sut = new NotificationSenderFactory(_mockServiceProvider.Object);
    }

    #region CreateSender Tests

    [Fact]
    public void CreateSender_WithEmailMethod_ReturnsEmailNotificationSender()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailNotificationSender>>();
        var emailSender = new EmailNotificationSender(mockLogger.Object);
        
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(EmailNotificationSender)))
            .Returns(emailSender);

        // Act
        var result = _sut.CreateSender(NotificationMethod.Email);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<EmailNotificationSender>(result);
    }

    [Fact]
    public void CreateSender_WithSmsMethod_ReturnsSmsNotificationSender()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SmsNotificationSender>>();
        var smsSender = new SmsNotificationSender(mockLogger.Object);
        
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(SmsNotificationSender)))
            .Returns(smsSender);

        // Act
        var result = _sut.CreateSender(NotificationMethod.Sms);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SmsNotificationSender>(result);
    }

    [Fact]
    public void CreateSender_WithPushMethod_ReturnsPushNotificationSender()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PushNotificationSender>>();
        var pushSender = new PushNotificationSender(mockLogger.Object);
        
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(PushNotificationSender)))
            .Returns(pushSender);

        // Act
        var result = _sut.CreateSender(NotificationMethod.Push);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<PushNotificationSender>(result);
    }

    [Fact]
    public void CreateSender_WithInvalidMethod_ThrowsArgumentException()
    {
        // Arrange
        var invalidMethod = (NotificationMethod)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _sut.CreateSender(invalidMethod));
        Assert.Contains("Unsupported notification method", exception.Message);
    }

    [Theory]
    [InlineData(NotificationMethod.Email)]
    [InlineData(NotificationMethod.Sms)]
    [InlineData(NotificationMethod.Push)]
    public void CreateSender_WithValidMethod_ReturnsINotificationSender(NotificationMethod method)
    {
        // Arrange
        SetupMockServiceProvider(method);

        // Act
        var result = _sut.CreateSender(method);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<INotificationSender>(result);
    }

    [Fact]
    public void CreateSender_CalledMultipleTimes_ReturnsDifferentInstances()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmailNotificationSender>>();
        
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(EmailNotificationSender)))
            .Returns(() => new EmailNotificationSender(mockLogger.Object));

        // Act
        var result1 = _sut.CreateSender(NotificationMethod.Email);
        var result2 = _sut.CreateSender(NotificationMethod.Email);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region Helper Methods

    private void SetupMockServiceProvider(NotificationMethod method)
    {
        switch (method)
        {
            case NotificationMethod.Email:
                var emailLogger = new Mock<ILogger<EmailNotificationSender>>();
                _mockServiceProvider
                    .Setup(x => x.GetService(typeof(EmailNotificationSender)))
                    .Returns(new EmailNotificationSender(emailLogger.Object));
                break;
            case NotificationMethod.Sms:
                var smsLogger = new Mock<ILogger<SmsNotificationSender>>();
                _mockServiceProvider
                    .Setup(x => x.GetService(typeof(SmsNotificationSender)))
                    .Returns(new SmsNotificationSender(smsLogger.Object));
                break;
            case NotificationMethod.Push:
                var pushLogger = new Mock<ILogger<PushNotificationSender>>();
                _mockServiceProvider
                    .Setup(x => x.GetService(typeof(PushNotificationSender)))
                    .Returns(new PushNotificationSender(pushLogger.Object));
                break;
        }
    }

    #endregion
}
