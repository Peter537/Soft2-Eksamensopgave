using Moq;
using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<ILegacyNotificationApiClient> _mockLegacyClient;
    private readonly NotificationService.Services.NotificationService _sut;

    public NotificationServiceTests()
    {
        _mockLegacyClient = new Mock<ILegacyNotificationApiClient>();
        _sut = new NotificationService.Services.NotificationService(_mockLegacyClient.Object);
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
        var expectedResponse = new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };

        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.SendNotificationAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Notification sent successfully", result.Message);
        _mockLegacyClient.Verify(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()), Times.Once);
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

        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new CustomerNotFoundException("Customer with ID 999 not found."));

        // Act & Assert
        await Assert.ThrowsAsync<CustomerNotFoundException>(
            () => _sut.SendNotificationAsync(request)
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

        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new NotificationFailedException("Legacy API unavailable."));

        // Act & Assert
        await Assert.ThrowsAsync<NotificationFailedException>(
            () => _sut.SendNotificationAsync(request)
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
        var expectedResponse = new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };

        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.SendNotificationAsync(request);

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
        var expectedResponse = new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };

        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _sut.SendNotificationAsync(request);

        // Assert
        _mockLegacyClient.Verify(
            x => x.SendNotificationAsync(It.Is<NotificationRequest>(r => r.CustomerId == customerId)),
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
        var expectedResponse = new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };

        _mockLegacyClient
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _sut.SendNotificationAsync(request);

        // Assert
        _mockLegacyClient.Verify(
            x => x.SendNotificationAsync(It.Is<NotificationRequest>(r =>
                r.CustomerId == 42 && r.Message == "Specific message content")),
            Times.Once);
    }

    #endregion
}
