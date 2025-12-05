using System.Net;
using System.Net.Http.Json;
using MToGo.NotificationService.Exceptions;
using MToGo.NotificationService.Models;

namespace MToGo.NotificationService.Clients;

public interface ILegacyNotificationApiClient
{
    /// <summary>
    /// Calls the legacy API to send a customer notification.
    /// </summary>
    Task<NotificationResponse> SendNotificationAsync(NotificationRequest request);
}

public class LegacyNotificationApiClient : ILegacyNotificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LegacyNotificationApiClient> _logger;

    public LegacyNotificationApiClient(HttpClient httpClient, ILogger<LegacyNotificationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Posts the notification request to the legacy endpoint and maps failures to domain exceptions.
    /// </summary>
    public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
    {
        _logger.LogInformation("Sending notification to customer {CustomerId}", request.CustomerId);

        // Gateway routes /api/v1/notifications/* to Legacy API
        var response = await _httpClient.PostAsJsonAsync("/api/v1/notifications/notify", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Customer {CustomerId} not found", request.CustomerId);
            throw new CustomerNotFoundException($"Customer with ID {request.CustomerId} not found.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send notification. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            throw new NotificationFailedException($"Failed to send notification: {errorContent}");
        }

        _logger.LogInformation("Notification sent successfully to customer {CustomerId}", request.CustomerId);
        
        return new NotificationResponse
        {
            Success = true,
            Message = "Notification sent successfully"
        };
    }
}
