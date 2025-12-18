using Moq;
using MToGo.NotificationService.Adapters;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationAdapter> _mockAdapter;
    private readonly NotificationService.Services.NotificationService _target;

    public NotificationServiceTests()
    {
        _mockAdapter = new Mock<INotificationAdapter>();
        _target = new NotificationService.Services.NotificationService(_mockAdapter.Object);
    }

    #region SendNotificationAsync Tests

    [Fact]
    public async Task SendNotificationAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = "Your order has been shipped!"
        };
        _mockAdapter
            .Setup(x => x.SendAsync(request.CustomerId, It.IsAny<string>(), request.Message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationAdapterResult.Succeeded("Notification sent successfully"));

        // Act
        var result = await _target.SendNotificationAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Notification sent successfully", result.Message);
        _mockAdapter.Verify(x => x.SendAsync(request.CustomerId, string.Empty, request.Message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithNonExistentCustomer_ThrowsCustomerNotFoundException()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 999,
            Message = "Test message"
        };

        _mockAdapter
            .Setup(x => x.SendAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationAdapterResult.Failed(NotificationAdapterError.CustomerNotFound, "Customer with ID 999 not found."));

        // Act & Assert
        await Assert.ThrowsAsync<CustomerNotFoundException>(
            () => _target.SendNotificationAsync(request)
        );
    }

    [Fact]
    public async Task SendNotificationAsync_WhenLegacyApiFails_ThrowsNotificationFailedException()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = "Test message"
        };

        _mockAdapter
            .Setup(x => x.SendAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationAdapterResult.Failed(NotificationAdapterError.ServiceUnavailable, "Legacy API unavailable."));

        // Act & Assert
        await Assert.ThrowsAsync<NotificationFailedException>(
            () => _target.SendNotificationAsync(request)
        );
    }

    [Theory]
    [InlineData("Order status: Preparing")]
    [InlineData("Order status: Out for delivery")]
    [InlineData("Order status: Delivered")]
    public async Task SendNotificationAsync_WithDifferentStatusMessages_Succeeds(string message)
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = message
        };
        _mockAdapter
            .Setup(x => x.SendAsync(request.CustomerId, It.IsAny<string>(), request.Message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationAdapterResult.Succeeded("Notification sent successfully"));

        // Act
        var result = await _target.SendNotificationAsync(request);

        // Assert
        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task SendNotificationAsync_WithDifferentCustomerIds_CallsLegacyClient(int customerId)
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Test notification"
        };
        _mockAdapter
            .Setup(x => x.SendAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationAdapterResult.Succeeded("Notification sent successfully"));

        // Act
        await _target.SendNotificationAsync(request);

        // Assert
        _mockAdapter.Verify(
            x => x.SendAsync(customerId, string.Empty, "Test notification", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_PassesRequestToLegacyClient()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 42,
            Message = "Specific message content"
        };
        _mockAdapter
            .Setup(x => x.SendAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationAdapterResult.Succeeded("Notification sent successfully"));

        // Act
        await _target.SendNotificationAsync(request);

        // Assert
        _mockAdapter.Verify(
            x => x.SendAsync(42, string.Empty, "Specific message content", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

