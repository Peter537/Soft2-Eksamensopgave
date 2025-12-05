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
                // Use the /menu endpoint which is AllowAnonymous
                var response = await _httpClient.GetAsync($"/api/v1/partners/{partnerId}/menu");

                if (response.IsSuccessStatusCode)
                {
                    var menuResponse = await response.Content.ReadFromJsonAsync<PartnerMenuResponse>();
                    if (menuResponse != null)
                    {
                        return new PartnerResponse
                        {
                            Id = menuResponse.PartnerId,
                            Name = menuResponse.PartnerName,
                            Address = menuResponse.PartnerAddress
                        };
                    }
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

    /// <summary>
    /// Internal DTO for deserializing the partner menu response
    /// </summary>
    internal class PartnerMenuResponse
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string PartnerAddress { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
