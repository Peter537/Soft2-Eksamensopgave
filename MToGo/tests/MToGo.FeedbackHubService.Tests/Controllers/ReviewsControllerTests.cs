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
}
