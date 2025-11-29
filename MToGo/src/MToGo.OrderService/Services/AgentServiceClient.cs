using MToGo.OrderService.Models;

namespace MToGo.OrderService.Services
{
    public interface IAgentServiceClient
    {
        Task<AgentResponse?> GetAgentByIdAsync(int agentId);
    }

    public class AgentServiceClient : IAgentServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentServiceClient> _logger;

        public AgentServiceClient(HttpClient httpClient, ILogger<AgentServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AgentResponse?> GetAgentByIdAsync(int agentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/agents/agents/{agentId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AgentResponse>();
                }

                _logger.LogWarning("Failed to get agent {AgentId}: {StatusCode}", agentId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching agent {AgentId} from Agent service via Gateway", agentId);
                return null;
            }
        }
    }
}
