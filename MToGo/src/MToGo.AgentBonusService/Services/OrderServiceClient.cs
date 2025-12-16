using MToGo.AgentBonusService.Models;
using System.Net.Http.Headers;

namespace MToGo.AgentBonusService.Services
{
    public interface IOrderServiceClient
    {
        Task<List<AgentOrderResponse>?> GetAgentOrdersAsync(int agentId, DateTime startDate, DateTime endDate, string? authToken = null);
    }

    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderServiceClient> _logger;

        public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<AgentOrderResponse>?> GetAgentOrdersAsync(int agentId, DateTime startDate, DateTime endDate, string? authToken = null)
        {
            try
            {
                var startDateStr = startDate.ToString("yyyy-MM-dd");
                var endDateStr = endDate.ToString("yyyy-MM-dd");
                
                var url = $"/api/v1/orders/agent/{agentId}?startDate={startDateStr}&endDate={endDateStr}";
                _logger.LogInformation("Fetching orders for agent {AgentId} from {StartDate} to {EndDate}", agentId, startDateStr, endDateStr);
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var orders = await response.Content.ReadFromJsonAsync<List<AgentOrderResponse>>();
                    _logger.LogInformation("Retrieved {Count} orders for agent {AgentId}", orders?.Count ?? 0, agentId);
                    return orders;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get orders for agent {AgentId}: {StatusCode} - {Error}", agentId, response.StatusCode, errorContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders for agent {AgentId} from Order service", agentId);
                return null;
            }
        }
    }
}
