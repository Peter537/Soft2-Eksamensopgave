using MToGo.AgentBonusService.Models;

namespace MToGo.AgentBonusService.Services
{
    public interface IAgentBonusService
    {
        Task<BonusCalculationResponse> CalculateBonusAsync(int agentId, DateTime startDate, DateTime endDate, string? authToken = null);
    }

    public class AgentBonusService : IAgentBonusService
    {
        private readonly IOrderServiceClient _orderServiceClient;
        private readonly IFeedbackHubClient _feedbackHubClient;
        private readonly IAgentServiceClient _agentServiceClient;
        private readonly ILogger<AgentBonusService> _logger;

        // Constants for bonus calculation
        private const decimal ContributionRate = 0.15m; // 15% of delivery fee goes to bonus pool
        private const int MinimumDeliveries = 20;
        private const int MinimumReviewsForRating = 5;
        private const decimal DefaultRating = 3.0m;
        private const decimal MaxRating = 5.0m;
        private const decimal TimeWeight = 0.5m;
        private const decimal ReviewWeight = 0.5m;
        private const decimal EarlyLateMultiplier = 1.2m;
        private const decimal NormalMultiplier = 1.0m;

        // Time boundaries (hour of day)
        private const int EarlyStartHour = 10;  // 10:00
        private const int EarlyEndHour = 14;    // 14:00
        private const int LateStartHour = 20;   // 20:00

        public AgentBonusService(
            IOrderServiceClient orderServiceClient,
            IFeedbackHubClient feedbackHubClient,
            IAgentServiceClient agentServiceClient,
            ILogger<AgentBonusService> logger)
        {
            _orderServiceClient = orderServiceClient;
            _feedbackHubClient = feedbackHubClient;
            _agentServiceClient = agentServiceClient;
            _logger = logger;
        }

        public async Task<BonusCalculationResponse> CalculateBonusAsync(int agentId, DateTime startDate, DateTime endDate, string? authToken = null)
        {
            _logger.LogInformation("Calculating bonus for agent {AgentId} from {StartDate} to {EndDate}", 
                agentId, startDate, endDate);

            var response = new BonusCalculationResponse
            {
                AgentId = agentId,
                Period = new BonusPeriod
                {
                    StartDate = startDate,
                    EndDate = endDate
                }
            };

            // Step 1: Validate agent exists
            var agent = await _agentServiceClient.GetAgentByIdAsync(agentId, authToken);
            if (agent == null)
            {
                response.Qualified = false;
                response.DisqualificationReason = $"Agent with ID {agentId} not found.";
                return response;
            }
            response.AgentName = agent.Name;

            // Step 2: Get orders for the period
            var orders = await _orderServiceClient.GetAgentOrdersAsync(agentId, startDate, endDate, authToken);
            if (orders == null)
            {
                response.Qualified = false;
                response.DisqualificationReason = "Failed to retrieve orders from Order Service.";
                response.Warnings.Add("Order Service unavailable or authentication failed");
                return response;
            }

            // Filter only delivered orders
            var deliveredOrders = orders.Where(o => o.Status == "Delivered").ToList();
            response.DeliveryCount = deliveredOrders.Count;

            // Step 3: Check minimum delivery qualification
            if (deliveredOrders.Count < MinimumDeliveries)
            {
                response.Qualified = false;
                response.DisqualificationReason = $"Minimum {MinimumDeliveries} deliveries required. Agent has {deliveredOrders.Count} deliveries.";
                return response;
            }

            // Step 4: Calculate delivery timing breakdown
            CalculateDeliveryTimings(deliveredOrders, response);

            // Step 5: Calculate contribution (15% of total delivery fees)
            response.TotalDeliveryFees = deliveredOrders.Sum(o => o.DeliveryFee);
            response.Contribution = Math.Round(response.TotalDeliveryFees * ContributionRate, 2, MidpointRounding.ToZero);

            // Step 6: Calculate time score
            response.TimeScore = CalculateTimeScore(response.EarlyDeliveries, response.NormalDeliveries, response.LateDeliveries);

            // Step 7: Get reviews and calculate review score
            var (reviews, serviceAvailable) = await _feedbackHubClient.GetAgentReviewsAsync(agentId, startDate, endDate, authToken);
            
            if (!serviceAvailable)
            {
                response.Warnings.Add("Feedback Hub unavailable - using default rating of 3.0/5");
                response.UsedDefaultRating = true;
                response.AverageRating = DefaultRating;
                response.ReviewCount = 0;
            }
            else if (reviews == null || reviews.Count < MinimumReviewsForRating)
            {
                response.UsedDefaultRating = true;
                response.AverageRating = DefaultRating;
                response.ReviewCount = reviews?.Count ?? 0;
                if (reviews != null && reviews.Count > 0)
                {
                    response.Warnings.Add($"Only {reviews.Count} reviews found (minimum {MinimumReviewsForRating}) - using default rating");
                }
            }
            else
            {
                response.ReviewCount = reviews.Count;
                response.AverageRating = Math.Round((decimal)reviews.Average(r => r.Ratings.Agent), 2);
                response.UsedDefaultRating = false;
            }

            response.ReviewScore = Math.Round(response.AverageRating / MaxRating, 4);

            // Step 8: Calculate performance score
            response.Performance = Math.Round(
                (TimeWeight * response.TimeScore) + (ReviewWeight * response.ReviewScore), 
                4);

            // Step 9: Calculate final bonus
            response.BonusAmount = Math.Round(response.Contribution * response.Performance, 2, MidpointRounding.ToZero);
            response.Qualified = true;

            _logger.LogInformation(
                "Bonus calculated for agent {AgentId}: {DeliveryCount} deliveries, Contribution={Contribution}, Performance={Performance}, Bonus={BonusAmount}",
                agentId, response.DeliveryCount, response.Contribution, response.Performance, response.BonusAmount);

            return response;
        }

        private void CalculateDeliveryTimings(List<AgentOrderResponse> orders, BonusCalculationResponse response)
        {
            foreach (var order in orders)
            {
                if (DateTime.TryParse(order.OrderCreatedTime, out var createdAt))
                {
                    var hour = createdAt.Hour;

                    if (hour >= EarlyStartHour && hour < EarlyEndHour)
                    {
                        response.EarlyDeliveries++;
                    }
                    else if (hour >= LateStartHour)
                    {
                        response.LateDeliveries++;
                    }
                    else
                    {
                        response.NormalDeliveries++;
                    }
                }
                else
                {
                    // If we can't parse the time, count as normal
                    response.NormalDeliveries++;
                    _logger.LogWarning("Could not parse OrderCreatedTime: {OrderCreatedTime}", order.OrderCreatedTime);
                }
            }
        }

        private decimal CalculateTimeScore(int early, int normal, int late)
        {
            var totalDeliveries = early + normal + late;
            if (totalDeliveries == 0) return 0m;

            // Calculate weighted deliveries
            var weightedDeliveries = 
                (early * EarlyLateMultiplier) + 
                (normal * NormalMultiplier) + 
                (late * EarlyLateMultiplier);

            // Maximum possible = all deliveries at 1.2x
            var maxWeighted = totalDeliveries * EarlyLateMultiplier;

            // Normalize to 0-1 scale
            var timeScore = weightedDeliveries / maxWeighted;
            
            return Math.Round(timeScore, 4);
        }
    }
}
