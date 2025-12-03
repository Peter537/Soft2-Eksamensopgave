using MToGo.AgentBonusService.Models;
using System.Net.Http.Headers;

namespace MToGo.AgentBonusService.Services
{
    public interface IAgentServiceClient
    {
        Task<AgentResponse?> GetAgentByIdAsync(int agentId, string? authToken = null);
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

        public async Task<AgentResponse?> GetAgentByIdAsync(int agentId, string? authToken = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/agents/{agentId}");
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AgentResponse>();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get agent {AgentId}: {StatusCode} - {Error}", agentId, response.StatusCode, errorContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching agent {AgentId} from Agent service", agentId);
                return null;
            }
        }
    }
}
