using Microsoft.AspNetCore.Mvc;
using Moq;
using MToGo.NotificationService.Controllers;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;
using MToGo.NotificationService.Services;

namespace MToGo.NotificationService.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly NotificationsController _target;

    public NotificationsControllerTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _target = new NotificationsController(_mockNotificationService.Object);
    }

    #region Notify Tests

    [Fact]
    public async Task Notify_WithValidRequest_Returns202Accepted()
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

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Notify(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
        var response = Assert.IsType<NotificationResponse>(acceptedResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Notify_WithNonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 999,
            Message = "Test message"
        };

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new CustomerNotFoundException("Customer with ID 999 not found."));

        // Act
        var result = await _target.Notify(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task Notify_WhenNotificationFails_Returns500InternalServerError()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = "Test message"
        };

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ThrowsAsync(new NotificationFailedException("Failed to send notification."));

        // Act
        var result = await _target.Notify(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Theory]
    [InlineData("Your order is ready for pickup!")]
    [InlineData("Order #12345 has been delivered.")]
    [InlineData("Your delivery is on its way!")]
    public async Task Notify_WithDifferentMessages_Returns202Accepted(string message)
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

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Notify(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    public async Task Notify_WithDifferentCustomerIds_Returns202Accepted(int customerId)
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

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _target.Notify(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
    }

    [Fact]
    public async Task Notify_CallsServiceWithCorrectRequest()
    {
        // Arrange
        var request = new NotificationRequest
        {
            CustomerId = 5,
            Message = "Specific test message"
        };
        var expectedResponse = new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };

        _mockNotificationService
            .Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        await _target.Notify(request);

        // Assert
        _mockNotificationService.Verify(
            x => x.SendNotificationAsync(It.Is<NotificationRequest>(r =>
                r.CustomerId == 5 && r.Message == "Specific test message")),
            Times.Once);
    }

    #endregion
}

