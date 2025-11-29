using MToGo.OrderService.Models;

namespace MToGo.OrderService.Services
{
    public interface IPartnerServiceClient
    {
        Task<PartnerResponse?> GetPartnerByIdAsync(int partnerId);
    }

    public class PartnerServiceClient : IPartnerServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PartnerServiceClient> _logger;

        public PartnerServiceClient(HttpClient httpClient, ILogger<PartnerServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PartnerResponse?> GetPartnerByIdAsync(int partnerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/partners/partners/{partnerId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PartnerResponse>();
                }

                _logger.LogWarning("Failed to get partner {PartnerId}: {StatusCode}", partnerId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fetching partner {PartnerId} from Partner service via Gateway", partnerId);
                return null;
            }
        }
    }
}
