using Microsoft.Extensions.Logging;
using Moq;
using MToGo.NotificationService.Adapters;
using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Tests.Adapters;

/// <summary>
/// Tests for the Legacy Notification Adapter (Adapter pattern).
/// </summary>
public class LegacyNotificationAdapterTests
{
    private readonly Mock<ILegacyNotificationApiClient> _mockLegacyClient;
    private readonly Mock<ILogger<LegacyNotificationAdapter>> _mockLogger;
    private readonly LegacyNotificationAdapter _target;

    public LegacyNotificationAdapterTests()
    {
        _mockLegacyClient = new Mock<ILegacyNotificationApiClient>();
        _mockLogger = new Mock<ILogger<LegacyNotificationAdapter>>();
        _target = new LegacyNotificationAdapter(_mockLegacyClient.Object, _mockLogger.Object);
    }

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var customerId = 1;
        var title = "Order Update";
        var body = "Your order is on its way!";
        
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(new NotificationResponse { Success = true, Message = "Sent" });

        // Act
        var result = await _target.SendAsync(customerId, title, body);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendAsync_CombinesTitleAndBodyInMessage()
    {
        // Arrange
        var customerId = 1;
        var title = "Test Title";
        var body = "Test Body";
        NotificationRequest? capturedRequest = null;
        
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .Callback<NotificationRequest>(r => capturedRequest = r)
            .ReturnsAsync(new NotificationResponse { Success = true });

        // Act
        await _target.SendAsync(customerId, title, body);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(customerId, capturedRequest.CustomerId);
        Assert.Contains(title, capturedRequest.Message);
        Assert.Contains(body, capturedRequest.Message);
    }

    [Fact]
    public async Task SendAsync_WithEmptyTitle_UsesOnlyBody()
    {
        // Arrange
        var customerId = 1;
        var title = "";
        var body = "Test Body";
        NotificationRequest? capturedRequest = null;
        
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .Callback<NotificationRequest>(r => capturedRequest = r)
            .ReturnsAsync(new NotificationResponse { Success = true });

        // Act
        await _target.SendAsync(customerId, title, body);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(body, capturedRequest.Message);
    }

    [Fact]
    public async Task SendAsync_WhenCustomerNotFound_ReturnsCustomerNotFoundError()
    {
        // Arrange
        var customerId = 999;
        
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new CustomerNotFoundException("Customer not found"));

        // Act
        var result = await _target.SendAsync(customerId, "Title", "Body");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(NotificationAdapterError.CustomerNotFound, result.Error);
    }

    [Fact]
    public async Task SendAsync_WhenNotificationFails_ReturnsServiceUnavailableError()
    {
        // Arrange
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new NotificationFailedException("Service error"));

        // Act
        var result = await _target.SendAsync(1, "Title", "Body");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(NotificationAdapterError.ServiceUnavailable, result.Error);
    }

    [Fact]
    public async Task SendAsync_WhenHttpRequestFails_ReturnsServiceUnavailableError()
    {
        // Arrange
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _target.SendAsync(1, "Title", "Body");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(NotificationAdapterError.ServiceUnavailable, result.Error);
    }

    [Fact]
    public async Task SendAsync_WhenUnexpectedError_ReturnsUnknownError()
    {
        // Arrange
        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected"));

        // Act
        var result = await _target.SendAsync(1, "Title", "Body");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(NotificationAdapterError.Unknown, result.Error);
    }

    #endregion


    #region NotificationAdapterResult Tests

    [Fact]
    public void NotificationAdapterResult_Succeeded_ReturnsCorrectResult()
    {
        // Act
        var result = NotificationAdapterResult.Succeeded("Custom message");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Custom message", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NotificationAdapterResult_Succeeded_WithoutMessage_UsesDefault()
    {
        // Act
        var result = NotificationAdapterResult.Succeeded();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Notification sent successfully", result.Message);
    }

    [Fact]
    public void NotificationAdapterResult_Failed_ReturnsCorrectResult()
    {
        // Act
        var result = NotificationAdapterResult.Failed(NotificationAdapterError.CustomerNotFound, "Not found");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(NotificationAdapterError.CustomerNotFound, result.Error);
        Assert.Equal("Not found", result.Message);
    }

    [Theory]
    [InlineData(NotificationAdapterError.CustomerNotFound)]
    [InlineData(NotificationAdapterError.ServiceUnavailable)]
    [InlineData(NotificationAdapterError.InvalidRequest)]
    [InlineData(NotificationAdapterError.Unknown)]
    public void NotificationAdapterResult_Failed_WithVariousErrors_SetsCorrectError(NotificationAdapterError error)
    {
        // Act
        var result = NotificationAdapterResult.Failed(error, "Error message");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(error, result.Error);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void LegacyNotificationAdapter_ImplementsINotificationAdapter()
    {
        // Assert
        Assert.IsAssignableFrom<INotificationAdapter>(_target);
    }

    #endregion
}

