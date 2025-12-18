extern alias NotificationServiceApp;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MToGo.NotificationService.Clients;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;
using MToGo.Testing;
using NotificationServiceProgram = NotificationServiceApp::Program;

namespace MToGo.NotificationService.Tests.Integration;

public class NotificationIntegrationTests : IClassFixture<WebApplicationFactory<NotificationServiceProgram>>
{
    private readonly WebApplicationFactory<NotificationServiceProgram> _factory;

    public NotificationIntegrationTests(WebApplicationFactory<NotificationServiceProgram> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMockedLegacyApi(Action<Mock<ILegacyNotificationApiClient>> setupMock, bool authenticated = false, string? userId = null, string? role = null)
    {
        var mockLegacyClient = new Mock<ILegacyNotificationApiClient>();
        setupMock(mockLegacyClient);

        if (authenticated)
        {
            TestAuthenticationHandler.SetTestUser(userId ?? "1", role ?? "Customer");
        }
        else
        {
            TestAuthenticationHandler.ClearTestUser();
        }

        return _factory.CreateAuthenticatedClient(services =>
        {
            // Remove existing registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ILegacyNotificationApiClient));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add mock
            services.AddSingleton(mockLegacyClient.Object);
        });
    }

    #region US-42: Send Status Notifications Acceptance Criteria Tests

    [Fact]
    public async Task Notify_GivenStatusChange_WhenTriggered_ThenNotificationSentViaLegacy_Returns202()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
                .ReturnsAsync(new NotificationResponse
                {
                    Success = true,
                    Message = "Notification sent successfully"
                });
        });

        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = "Your order status has changed to: Out for delivery"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Notify_WithNonExistentCustomer_Returns404NotFound()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
                .ThrowsAsync(new CustomerNotFoundException("Customer with ID 999 not found."));
        });

        var request = new NotificationRequest
        {
            CustomerId = 999,
            Message = "Test notification"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Notify_WhenLegacyServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
                .ThrowsAsync(new NotificationFailedException("Legacy notification service unavailable."));
        });

        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = "Test notification"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    #endregion

    #region Additional Notification Tests

    [Theory]
    [InlineData("Order placed successfully")]
    [InlineData("Order is being prepared")]
    [InlineData("Order is out for delivery")]
    [InlineData("Order has been delivered")]
    public async Task Notify_WithDifferentStatusMessages_Returns202Accepted(string statusMessage)
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
                .ReturnsAsync(new NotificationResponse
                {
                    Success = true,
                    Message = "Notification sent successfully"
                });
        });

        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = statusMessage
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    public async Task Notify_WithDifferentCustomerIds_Returns202Accepted(int customerId)
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
                .ReturnsAsync(new NotificationResponse
                {
                    Success = true,
                    Message = "Notification sent successfully"
                });
        });

        var request = new NotificationRequest
        {
            CustomerId = customerId,
            Message = "Status update notification"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Notify_ReturnsNotificationResponse()
    {
        // Arrange
        var client = CreateClientWithMockedLegacyApi(mock =>
        {
            mock.Setup(x => x.SendNotificationAsync(It.IsAny<NotificationRequest>()))
                .ReturnsAsync(new NotificationResponse
                {
                    Success = true,
                    Message = "Notification sent successfully"
                });
        });

        var request = new NotificationRequest
        {
            CustomerId = 1,
            Message = "Test notification"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/notifications", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Notification sent successfully", result.Message);
    }

    #endregion
}

