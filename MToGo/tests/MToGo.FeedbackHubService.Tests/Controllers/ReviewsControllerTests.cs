using Microsoft.AspNetCore.Mvc;
using Moq;
using MToGo.FeedbackHubService.Controllers;
using MToGo.FeedbackHubService.Exceptions;
using MToGo.FeedbackHubService.Models;
using MToGo.FeedbackHubService.Services;

namespace MToGo.FeedbackHubService.Tests.Controllers;

public class ReviewsControllerTests
{
    private readonly Mock<IReviewService> _mockReviewService;
    private readonly ReviewsController _controller;

    public ReviewsControllerTests()
    {
        _mockReviewService = new Mock<IReviewService>();
        _controller = new ReviewsController(_mockReviewService.Object);
    }

    #region CreateReview Tests

    [Fact]
    public async Task CreateReview_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            AgentId = 30,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = "Delicious!",
            AgentComment = "Fast delivery",
            OrderComment = "Great experience"
        };

        var response = new ReviewResponse
        {
            Id = 1,
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            AgentId = 30,
            CreatedAt = DateTime.UtcNow,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = "Delicious!",
            AgentComment = "Fast delivery",
            OrderComment = "Great experience"
        };

        _mockReviewService
            .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateReview(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ReviewsController.GetReviewByOrderId), createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues?["orderId"]);
        var returnedReview = Assert.IsType<ReviewResponse>(createdResult.Value);
        Assert.Equal(1, returnedReview.Id);
        Assert.Equal(5, returnedReview.FoodRating);
    }

    [Fact]
    public async Task CreateReview_DuplicateReview_ReturnsConflict()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        _mockReviewService
            .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
            .ThrowsAsync(new DuplicateReviewException(1));

        // Act
        var result = await _controller.CreateReview(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task CreateReview_InvalidRating_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 6, // Invalid
            AgentRating = 4,
            OrderRating = 5
        };

        _mockReviewService
            .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
            .ThrowsAsync(new InvalidRatingException("Food", 6));

        // Act
        var result = await _controller.CreateReview(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateReview_CommentTooLong_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = new string('a', 501)
        };

        _mockReviewService
            .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
            .ThrowsAsync(new CommentTooLongException("Food", 501));

        // Act
        var result = await _controller.CreateReview(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task CreateReview_MinimalValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 1,
            AgentRating = 1,
            OrderRating = 1
        };

        var response = new ReviewResponse
        {
            Id = 1,
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            CreatedAt = DateTime.UtcNow,
            FoodRating = 1,
            AgentRating = 1,
            OrderRating = 1
        };

        _mockReviewService
            .Setup(s => s.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateReview(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedReview = Assert.IsType<ReviewResponse>(createdResult.Value);
        Assert.Equal(1, returnedReview.FoodRating);
        Assert.Null(returnedReview.FoodComment);
    }

    #endregion

    #region GetReviewByOrderId Tests

    [Fact]
    public async Task GetReviewByOrderId_ExistingReview_ReturnsOk()
    {
        // Arrange
        var response = new ReviewResponse
        {
            Id = 1,
            OrderId = 42,
            CustomerId = 10,
            PartnerId = 20,
            CreatedAt = DateTime.UtcNow,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        _mockReviewService
            .Setup(s => s.GetReviewByOrderIdAsync(42))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetReviewByOrderId(42);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReview = Assert.IsType<ReviewResponse>(okResult.Value);
        Assert.Equal(42, returnedReview.OrderId);
    }

    [Fact]
    public async Task GetReviewByOrderId_NonExistingReview_ReturnsNotFound()
    {
        // Arrange
        _mockReviewService
            .Setup(s => s.GetReviewByOrderIdAsync(999))
            .ReturnsAsync((ReviewResponse?)null);

        // Act
        var result = await _controller.GetReviewByOrderId(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region CheckReviewExists Tests

    [Fact]
    public async Task CheckReviewExists_ReviewExists_ReturnsOkWithTrue()
    {
        // Arrange
        _mockReviewService
            .Setup(s => s.HasReviewForOrderAsync(42))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckReviewExists(42);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CheckReviewExists_ReviewDoesNotExist_ReturnsOkWithFalse()
    {
        // Arrange
        _mockReviewService
            .Setup(s => s.HasReviewForOrderAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckReviewExists(999);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetAllReviews Tests

    [Fact]
    public async Task GetAllReviews_NoFilters_ReturnsOkWithAllReviews()
    {
        // Arrange
        var reviews = new List<OrderReviewResponse>
        {
            new OrderReviewResponse
            {
                AgentId = 1,
                PartnerId = 10,
                CustomerId = 100,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Ratings = new RatingsDto { Food = 5, Agent = 4, Order = 5 },
                Comments = new CommentsDto { Food = "Great", Agent = "Fast", Order = "Perfect" }
            }
        };

        _mockReviewService
            .Setup(s => s.GetAllReviewsAsync(null, null, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetAllReviews(null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReviews = Assert.IsAssignableFrom<IEnumerable<OrderReviewResponse>>(okResult.Value);
        Assert.Single(returnedReviews);
    }

    [Fact]
    public async Task GetAllReviews_WithDateFilters_PassesFiltersToService()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var reviews = new List<OrderReviewResponse>();

        _mockReviewService
            .Setup(s => s.GetAllReviewsAsync(startDate, endDate, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetAllReviews(startDate, endDate, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockReviewService.Verify(s => s.GetAllReviewsAsync(startDate, endDate, null), Times.Once);
    }

    [Fact]
    public async Task GetAllReviews_WithAmountLimit_PassesLimitToService()
    {
        // Arrange
        var reviews = new List<OrderReviewResponse>();

        _mockReviewService
            .Setup(s => s.GetAllReviewsAsync(null, null, 10))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetAllReviews(null, null, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockReviewService.Verify(s => s.GetAllReviewsAsync(null, null, 10), Times.Once);
    }

    #endregion

    #region GetReviewsByAgent Tests

    [Fact]
    public async Task GetReviewsByAgent_ValidAgentId_ReturnsOkWithReviews()
    {
        // Arrange
        var agentId = 5;
        var reviews = new List<AgentReviewResponse>
        {
            new AgentReviewResponse
            {
                OrderId = 1,
                PartnerId = 10,
                CustomerId = 100,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Ratings = new RatingsDto { Food = 5, Agent = 4, Order = 5 },
                Comments = new CommentsDto()
            }
        };

        _mockReviewService
            .Setup(s => s.GetReviewsByAgentIdAsync(agentId, null, null, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsByAgent(agentId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReviews = Assert.IsAssignableFrom<IEnumerable<AgentReviewResponse>>(okResult.Value);
        Assert.Single(returnedReviews);
    }

    [Fact]
    public async Task GetReviewsByAgent_NoReviews_ReturnsOkWithEmptyList()
    {
        // Arrange
        var agentId = 999;
        var reviews = new List<AgentReviewResponse>();

        _mockReviewService
            .Setup(s => s.GetReviewsByAgentIdAsync(agentId, null, null, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsByAgent(agentId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReviews = Assert.IsAssignableFrom<IEnumerable<AgentReviewResponse>>(okResult.Value);
        Assert.Empty(returnedReviews);
    }

    #endregion

    #region GetReviewsByCustomer Tests

    [Fact]
    public async Task GetReviewsByCustomer_ValidCustomerId_ReturnsOkWithReviews()
    {
        // Arrange
        var customerId = 100;
        var reviews = new List<CustomerReviewResponse>
        {
            new CustomerReviewResponse
            {
                OrderId = 1,
                AgentId = 5,
                PartnerId = 10,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Ratings = new RatingsDto { Food = 5, Agent = 4, Order = 5 },
                Comments = new CommentsDto()
            }
        };

        _mockReviewService
            .Setup(s => s.GetReviewsByCustomerIdAsync(customerId, null, null, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsByCustomer(customerId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReviews = Assert.IsAssignableFrom<IEnumerable<CustomerReviewResponse>>(okResult.Value);
        Assert.Single(returnedReviews);
    }

    [Fact]
    public async Task GetReviewsByCustomer_WithAllFilters_PassesAllFiltersToService()
    {
        // Arrange
        var customerId = 100;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var amount = 5;
        var reviews = new List<CustomerReviewResponse>();

        _mockReviewService
            .Setup(s => s.GetReviewsByCustomerIdAsync(customerId, startDate, endDate, amount))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsByCustomer(customerId, startDate, endDate, amount);

        // Assert
        _mockReviewService.Verify(s => s.GetReviewsByCustomerIdAsync(customerId, startDate, endDate, amount), Times.Once);
    }

    #endregion

    #region GetReviewsByPartner Tests

    [Fact]
    public async Task GetReviewsByPartner_ValidPartnerId_ReturnsOkWithReviews()
    {
        // Arrange
        var partnerId = 10;
        var reviews = new List<PartnerReviewResponse>
        {
            new PartnerReviewResponse
            {
                OrderId = 1,
                AgentId = 5,
                CustomerId = 100,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Ratings = new RatingsDto { Food = 5, Agent = 4, Order = 5 },
                Comments = new CommentsDto()
            }
        };

        _mockReviewService
            .Setup(s => s.GetReviewsByPartnerIdAsync(partnerId, null, null, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsByPartner(partnerId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReviews = Assert.IsAssignableFrom<IEnumerable<PartnerReviewResponse>>(okResult.Value);
        Assert.Single(returnedReviews);
    }

    [Fact]
    public async Task GetReviewsByPartner_MultipleReviews_ReturnsAllReviews()
    {
        // Arrange
        var partnerId = 10;
        var reviews = new List<PartnerReviewResponse>
        {
            new PartnerReviewResponse
            {
                OrderId = 1,
                AgentId = 5,
                CustomerId = 100,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Ratings = new RatingsDto { Food = 5, Agent = 4, Order = 5 },
                Comments = new CommentsDto()
            },
            new PartnerReviewResponse
            {
                OrderId = 2,
                AgentId = 6,
                CustomerId = 101,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Ratings = new RatingsDto { Food = 3, Agent = 3, Order = 3 },
                Comments = new CommentsDto()
            }
        };

        _mockReviewService
            .Setup(s => s.GetReviewsByPartnerIdAsync(partnerId, null, null, null))
            .ReturnsAsync(reviews);

        // Act
        var result = await _controller.GetReviewsByPartner(partnerId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReviews = Assert.IsAssignableFrom<IEnumerable<PartnerReviewResponse>>(okResult.Value);
        Assert.Equal(2, returnedReviews.Count());
    }

    #endregion

    #region GetReviewForOrder Tests

    [Fact]
    public async Task GetReviewForOrder_ExistingReview_ReturnsOk()
    {
        // Arrange
        var response = new ReviewResponse
        {
            Id = 1,
            OrderId = 42,
            CustomerId = 10,
            PartnerId = 20,
            CreatedAt = DateTime.UtcNow,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        _mockReviewService
            .Setup(s => s.GetReviewByOrderIdAsync(42))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetReviewForOrder(42);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReview = Assert.IsType<ReviewResponse>(okResult.Value);
        Assert.Equal(42, returnedReview.OrderId);
    }

    [Fact]
    public async Task GetReviewForOrder_NonExistingReview_ReturnsNotFound()
    {
        // Arrange
        _mockReviewService
            .Setup(s => s.GetReviewByOrderIdAsync(999))
            .ReturnsAsync((ReviewResponse?)null);

        // Act
        var result = await _controller.GetReviewForOrder(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion
}

