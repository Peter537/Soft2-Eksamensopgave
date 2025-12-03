using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.AgentBonusService.Models;
using MToGo.AgentBonusService.Services;

namespace MToGo.AgentBonusService.Tests.Services;

public class AgentBonusServiceTests
{
    private readonly Mock<IOrderServiceClient> _orderClientMock;
    private readonly Mock<IFeedbackHubClient> _feedbackClientMock;
    private readonly Mock<IAgentServiceClient> _agentClientMock;
    private readonly Mock<ILogger<AgentBonusService.Services.AgentBonusService>> _loggerMock;
    private readonly IAgentBonusService _sut;

    public AgentBonusServiceTests()
    {
        _orderClientMock = new Mock<IOrderServiceClient>();
        _feedbackClientMock = new Mock<IFeedbackHubClient>();
        _agentClientMock = new Mock<IAgentServiceClient>();
        _loggerMock = new Mock<ILogger<AgentBonusService.Services.AgentBonusService>>();

        _sut = new AgentBonusService.Services.AgentBonusService(
            _orderClientMock.Object,
            _feedbackClientMock.Object,
            _agentClientMock.Object,
            _loggerMock.Object
        );
    }

    #region Agent Validation Tests

    [Fact]
    public async Task CalculateBonusAsync_WhenAgentNotFound_ReturnsNotQualified()
    {
        // Arrange
        var agentId = 999;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        _agentClientMock
            .Setup(x => x.GetAgentByIdAsync(agentId, It.IsAny<string?>()))
            .ReturnsAsync((AgentResponse?)null);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.Should().NotBeNull();
        result.Qualified.Should().BeFalse();
        result.DisqualificationReason.Should().Contain("not found");
        result.AgentId.Should().Be(agentId);
    }

    [Fact]
    public async Task CalculateBonusAsync_WhenAgentExists_SetsAgentName()
    {
        // Arrange
        var agentId = 1;
        var agentName = "John Delivery";
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        SetupAgentExists(agentId, agentName);
        SetupOrdersReturned(agentId, new List<AgentOrderResponse>());
        
        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.AgentName.Should().Be(agentName);
    }

    #endregion

    #region Order Retrieval Tests

    [Fact]
    public async Task CalculateBonusAsync_WhenOrderServiceFails_ReturnsNotQualifiedWithWarning()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        SetupAgentExists(agentId, "Test Agent");
        _orderClientMock
            .Setup(x => x.GetAgentOrdersAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .ReturnsAsync((List<AgentOrderResponse>?)null);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.Qualified.Should().BeFalse();
        result.DisqualificationReason.Should().Contain("Failed to retrieve orders");
        result.Warnings.Should().Contain(w => w.Contains("Order Service"));
    }

    #endregion

    #region Minimum Deliveries Tests

    [Fact]
    public async Task CalculateBonusAsync_WithLessThan20Deliveries_ReturnsNotQualified()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);
        var orders = CreateDeliveredOrders(19); // Less than minimum 20

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.Qualified.Should().BeFalse();
        result.DeliveryCount.Should().Be(19);
        result.DisqualificationReason.Should().Contain("Minimum 20 deliveries required");
    }

    [Fact]
    public async Task CalculateBonusAsync_WithExactly20Deliveries_ReturnsQualified()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);
        var orders = CreateDeliveredOrders(20);

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.Qualified.Should().BeTrue();
        result.DeliveryCount.Should().Be(20);
    }

    [Fact]
    public async Task CalculateBonusAsync_OnlyCountsDeliveredOrders()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var orders = new List<AgentOrderResponse>
        {
            CreateOrder(1, "Delivered", 29.00m),
            CreateOrder(2, "Pending", 29.00m),
            CreateOrder(3, "Delivered", 29.00m),
            CreateOrder(4, "Cancelled", 29.00m),
            CreateOrder(5, "InProgress", 29.00m)
        };

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.DeliveryCount.Should().Be(2); // Only "Delivered" orders count
    }

    #endregion

    #region Contribution Calculation Tests

    [Fact]
    public async Task CalculateBonusAsync_CalculatesContributionAs15PercentOfDeliveryFees()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);
        
        // 20 orders at 29.00 each = 580.00 total, 15% = 87.00
        var orders = CreateDeliveredOrders(20, deliveryFee: 29.00m);

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.TotalDeliveryFees.Should().Be(580.00m);
        result.Contribution.Should().Be(87.00m); // 580 * 0.15 = 87
    }

    #endregion

    #region Time Score Tests

    [Fact]
    public async Task CalculateBonusAsync_CategorizesEarlyDeliveries_Between10And14()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 12, 0, 0)) // 12:00 = Early
        ).ToList();

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.EarlyDeliveries.Should().Be(20);
        result.NormalDeliveries.Should().Be(0);
        result.LateDeliveries.Should().Be(0);
    }

    [Fact]
    public async Task CalculateBonusAsync_CategorizesNormalDeliveries_Between14And20()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 17, 0, 0)) // 17:00 = Normal
        ).ToList();

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.EarlyDeliveries.Should().Be(0);
        result.NormalDeliveries.Should().Be(20);
        result.LateDeliveries.Should().Be(0);
    }

    [Fact]
    public async Task CalculateBonusAsync_CategorizesLateDeliveries_After20()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 21, 0, 0)) // 21:00 = Late
        ).ToList();

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.EarlyDeliveries.Should().Be(0);
        result.NormalDeliveries.Should().Be(0);
        result.LateDeliveries.Should().Be(20);
    }

    [Fact]
    public async Task CalculateBonusAsync_TimeScore_MaxWhenAllEarlyOrLate()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        // All early deliveries should give max time score (1.0)
        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 11, 0, 0))
        ).ToList();

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.TimeScore.Should().Be(1.0m); // All early = max score
    }

    #endregion

    #region Review Score Tests

    [Fact]
    public async Task CalculateBonusAsync_WhenFeedbackHubUnavailable_UsesDefaultRating()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);
        var orders = CreateDeliveredOrders(20);

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.UsedDefaultRating.Should().BeTrue();
        result.AverageRating.Should().Be(3.0m); // Default rating
        result.ReviewScore.Should().Be(0.6m); // 3.0 / 5.0
        result.Warnings.Should().Contain(w => w.Contains("Feedback Hub unavailable"));
    }

    [Fact]
    public async Task CalculateBonusAsync_WithLessThan5Reviews_UsesDefaultRating()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);
        var orders = CreateDeliveredOrders(20);
        var reviews = CreateReviews(4, averageRating: 5); // Only 4 reviews

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupReviewsReturned(agentId, reviews);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.UsedDefaultRating.Should().BeTrue();
        result.ReviewCount.Should().Be(4);
        result.AverageRating.Should().Be(3.0m); // Default, not the 5.0 from reviews
    }

    [Fact]
    public async Task CalculateBonusAsync_With5OrMoreReviews_UsesActualRating()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);
        var orders = CreateDeliveredOrders(20);
        var reviews = CreateReviews(10, averageRating: 4);

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupReviewsReturned(agentId, reviews);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.UsedDefaultRating.Should().BeFalse();
        result.ReviewCount.Should().Be(10);
        result.AverageRating.Should().Be(4.0m);
        result.ReviewScore.Should().Be(0.8m); // 4.0 / 5.0
    }

    #endregion

    #region Performance Calculation Tests

    [Fact]
    public async Task CalculateBonusAsync_PerformanceIs50PercentTimeAnd50PercentReview()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        // All early deliveries (TimeScore = 1.0)
        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 11, 0, 0))
        ).ToList();
        
        // Perfect reviews (ReviewScore = 1.0)
        var reviews = CreateReviews(10, averageRating: 5);

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupReviewsReturned(agentId, reviews);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.TimeScore.Should().Be(1.0m);
        result.ReviewScore.Should().Be(1.0m);
        result.Performance.Should().Be(1.0m); // (0.5 * 1.0) + (0.5 * 1.0) = 1.0
    }

    #endregion

    #region Bonus Amount Calculation Tests

    [Fact]
    public async Task CalculateBonusAsync_BonusIsContributionTimesPerformance()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        // 20 orders at 29.00 each, all early, perfect reviews
        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 11, 0, 0))
        ).ToList();
        var reviews = CreateReviews(10, averageRating: 5);

        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupReviewsReturned(agentId, reviews);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        // Contribution = 580 * 0.15 = 87.00
        // Performance = 1.0
        // Bonus = 87.00 * 1.0 = 87.00
        result.BonusAmount.Should().Be(87.00m);
    }

    [Fact]
    public async Task CalculateBonusAsync_BonusReducedByLowerPerformance()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        // All normal deliveries (TimeScore ~0.833)
        var orders = Enumerable.Range(1, 20).Select(i => 
            CreateOrder(i, "Delivered", 29.00m, new DateTime(2025, 12, 1, 17, 0, 0))
        ).ToList();
        
        // Default rating (ReviewScore = 0.6)
        SetupAgentExists(agentId, "Test Agent");
        SetupOrdersReturned(agentId, orders);
        SetupFeedbackHubUnavailable(agentId);

        // Act
        var result = await _sut.CalculateBonusAsync(agentId, startDate, endDate, null);

        // Assert
        result.Contribution.Should().Be(87.00m);
        result.Performance.Should().BeLessThan(1.0m);
        result.BonusAmount.Should().BeLessThan(87.00m);
    }

    #endregion

    #region Helper Methods

    private void SetupAgentExists(int agentId, string name)
    {
        _agentClientMock
            .Setup(x => x.GetAgentByIdAsync(agentId, It.IsAny<string?>()))
            .ReturnsAsync(new AgentResponse { Id = agentId, Name = name, Email = "test@test.com" });
    }

    private void SetupOrdersReturned(int agentId, List<AgentOrderResponse> orders)
    {
        _orderClientMock
            .Setup(x => x.GetAgentOrdersAsync(agentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .ReturnsAsync(orders);
    }

    private void SetupFeedbackHubUnavailable(int agentId)
    {
        _feedbackClientMock
            .Setup(x => x.GetAgentReviewsAsync(agentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .ReturnsAsync((null as List<AgentReviewResponse>, false));
    }

    private void SetupReviewsReturned(int agentId, List<AgentReviewResponse> reviews)
    {
        _feedbackClientMock
            .Setup(x => x.GetAgentReviewsAsync(agentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .ReturnsAsync((reviews, true));
    }

    private List<AgentOrderResponse> CreateDeliveredOrders(int count, decimal deliveryFee = 29.00m)
    {
        return Enumerable.Range(1, count).Select(i => 
            CreateOrder(i, "Delivered", deliveryFee, new DateTime(2025, 12, 1, 14, 0, 0))
        ).ToList();
    }

    private AgentOrderResponse CreateOrder(int id, string status, decimal deliveryFee, DateTime? createdAt = null)
    {
        return new AgentOrderResponse
        {
            Id = id,
            Status = status,
            DeliveryFee = deliveryFee,
            OrderCreatedTime = (createdAt ?? DateTime.Now).ToString("O"),
            CustomerId = 1,
            PartnerId = 1,
            DeliveryAddress = "Test Address",
            ServiceFee = 10.00m,
            Items = new List<AgentOrderItemResponse>()
        };
    }

    private List<AgentReviewResponse> CreateReviews(int count, int averageRating)
    {
        return Enumerable.Range(1, count).Select(i => new AgentReviewResponse
        {
            OrderId = i,
            CustomerId = 1,
            PartnerId = 1,
            Ratings = new ReviewRatings { Agent = averageRating }
        }).ToList();
    }

    #endregion
}
