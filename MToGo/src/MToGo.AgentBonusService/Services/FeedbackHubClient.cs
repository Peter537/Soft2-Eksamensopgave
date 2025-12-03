using MToGo.AgentBonusService.Models;
using System.Net.Http.Headers;

namespace MToGo.AgentBonusService.Services
{
    public interface IFeedbackHubClient
    {
        Task<(List<AgentReviewResponse>? Reviews, bool ServiceAvailable)> GetAgentReviewsAsync(int agentId, DateTime startDate, DateTime endDate, string? authToken = null);
    }

    public class FeedbackHubClient : IFeedbackHubClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FeedbackHubClient> _logger;

        public FeedbackHubClient(HttpClient httpClient, ILogger<FeedbackHubClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(List<AgentReviewResponse>? Reviews, bool ServiceAvailable)> GetAgentReviewsAsync(int agentId, DateTime startDate, DateTime endDate, string? authToken = null)
        {
            try
            {
                var startDateStr = startDate.ToString("yyyy-MM-dd");
                var endDateStr = endDate.ToString("yyyy-MM-dd");
                
                var url = $"/api/v1/feedback-hub/reviews/agents/{agentId}?startDate={startDateStr}&endDate={endDateStr}";
                _logger.LogInformation("Fetching reviews for agent {AgentId} from {StartDate} to {EndDate}", agentId, startDateStr, endDateStr);
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                }
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var reviews = await response.Content.ReadFromJsonAsync<List<AgentReviewResponse>>();
                    _logger.LogInformation("Retrieved {Count} reviews for agent {AgentId}", reviews?.Count ?? 0, agentId);
                    return (reviews, true);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Agent has no reviews - this is valid, return empty list
                    _logger.LogInformation("No reviews found for agent {AgentId}", agentId);
                    return (new List<AgentReviewResponse>(), true);
                }

                _logger.LogWarning("Failed to get reviews for agent {AgentId}: {StatusCode}", agentId, response.StatusCode);
                return (null, false);
            }
            catch (HttpRequestException ex)
            {
                // Service unavailable - graceful fallback
                _logger.LogWarning(ex, "Feedback Hub service unavailable for agent {AgentId}", agentId);
                return (null, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews for agent {AgentId} from Feedback Hub", agentId);
                return (null, false);
            }
        }
    }
}
