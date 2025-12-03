using Moq;
using MToGo.FeedbackHubService.Entities;
using MToGo.FeedbackHubService.Exceptions;
using MToGo.FeedbackHubService.Models;
using MToGo.FeedbackHubService.Repositories;
using MToGo.FeedbackHubService.Services;

namespace MToGo.FeedbackHubService.Tests.Services;

public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _mockRepository;
    private readonly ReviewService _service;

    public ReviewServiceTests()
    {
        _mockRepository = new Mock<IReviewRepository>();
        _service = new ReviewService(_mockRepository.Object);
    }

    #region CreateReviewAsync Tests

    [Fact]
    public async Task CreateReviewAsync_ValidRequest_ReturnsReviewResponse()
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

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.OrderId);
        Assert.Equal(10, result.CustomerId);
        Assert.Equal(20, result.PartnerId);
        Assert.Equal(30, result.AgentId);
        Assert.Equal(5, result.FoodRating);
        Assert.Equal(4, result.AgentRating);
        Assert.Equal(5, result.OrderRating);
        Assert.Equal("Delicious!", result.FoodComment);
    }

    [Fact]
    public async Task CreateReviewAsync_AllRatingsZero_ThrowsNoRatingsProvidedException()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 0,
            AgentRating = 0,
            OrderRating = 0
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NoRatingsProvidedException>(
            () => _service.CreateReviewAsync(request));
    }

    [Fact]
    public async Task CreateReviewAsync_OnlyFoodRating_Succeeds()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 0,
            OrderRating = 0
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.FoodRating);
        Assert.Equal(0, result.AgentRating);
        Assert.Equal(0, result.OrderRating);
    }

    [Fact]
    public async Task CreateReviewAsync_DuplicateOrder_ThrowsDuplicateReviewException()
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

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DuplicateReviewException>(
            () => _service.CreateReviewAsync(request));
        Assert.Equal(1, exception.OrderId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public async Task CreateReviewAsync_InvalidFoodRating_ThrowsInvalidRatingException(int rating)
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = rating,
            AgentRating = 4,
            OrderRating = 5
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRatingException>(
            () => _service.CreateReviewAsync(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task CreateReviewAsync_InvalidAgentRating_ThrowsInvalidRatingException(int rating)
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = rating,
            OrderRating = 5
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRatingException>(
            () => _service.CreateReviewAsync(request));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task CreateReviewAsync_InvalidOrderRating_ThrowsInvalidRatingException(int rating)
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = rating
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRatingException>(
            () => _service.CreateReviewAsync(request));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task CreateReviewAsync_ValidRatings_AcceptsAllValidValues(int rating)
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = rating,
            AgentRating = rating,
            OrderRating = rating
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.Equal(rating, result.FoodRating);
        Assert.Equal(rating, result.AgentRating);
        Assert.Equal(rating, result.OrderRating);
    }

    [Fact]
    public async Task CreateReviewAsync_NullComments_AcceptsNullValues()
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
            FoodComment = null,
            AgentComment = null,
            OrderComment = null
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.Null(result.FoodComment);
        Assert.Null(result.AgentComment);
        Assert.Null(result.OrderComment);
    }

    [Fact]
    public async Task CreateReviewAsync_EmptyComments_ConvertsToNull()
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
            FoodComment = "   ",
            AgentComment = "",
            OrderComment = "  \t  "
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        Review? capturedReview = null;
        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .Callback<Review>(r => capturedReview = r)
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.Null(capturedReview?.FoodComment);
        Assert.Null(capturedReview?.AgentComment);
        Assert.Null(capturedReview?.OrderComment);
    }

    [Fact]
    public async Task CreateReviewAsync_CommentWithHtmlTags_SanitizesInput()
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
            FoodComment = "<script>alert('xss')</script>Good food!"
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        Review? capturedReview = null;
        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .Callback<Review>(r => capturedReview = r)
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        await _service.CreateReviewAsync(request);

        // Assert
        Assert.NotNull(capturedReview);
        Assert.DoesNotContain("<script>", capturedReview.FoodComment);
        Assert.DoesNotContain("</script>", capturedReview.FoodComment);
        Assert.Contains("Good food!", capturedReview.FoodComment);
    }

    [Fact]
    public async Task CreateReviewAsync_CommentTrimsWhitespace()
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
            FoodComment = "   Good food!   "
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        Review? capturedReview = null;
        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .Callback<Review>(r => capturedReview = r)
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        await _service.CreateReviewAsync(request);

        // Assert
        Assert.NotNull(capturedReview);
        Assert.Equal("Good food!", capturedReview.FoodComment);
    }

    [Fact]
    public async Task CreateReviewAsync_CommentExactly500Chars_Succeeds()
    {
        // Arrange
        var comment = new string('a', 500);
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = comment
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateReviewAsync_CommentOver500CharsAfterSanitization_ThrowsException()
    {
        // Arrange - 501 chars that won't be reduced by sanitization
        var comment = new string('a', 501);
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = comment
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<CommentTooLongException>(
            () => _service.CreateReviewAsync(request));
    }

    [Fact]
    public async Task CreateReviewAsync_WithoutAgentId_Succeeds()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            AgentId = null,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(1))
            .ReturnsAsync(false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) =>
            {
                r.Id = 1;
                return r;
            });

        // Act
        var result = await _service.CreateReviewAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AgentId);
    }

    #endregion

    #region GetReviewByOrderIdAsync Tests

    [Fact]
    public async Task GetReviewByOrderIdAsync_ExistingReview_ReturnsReviewResponse()
    {
        // Arrange
        var review = new Review
        {
            Id = 1,
            OrderId = 42,
            CustomerId = 10,
            PartnerId = 20,
            AgentId = 30,
            CreatedAt = DateTime.UtcNow,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = "Yummy!"
        };

        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(42))
            .ReturnsAsync(review);

        // Act
        var result = await _service.GetReviewByOrderIdAsync(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.OrderId);
        Assert.Equal(5, result.FoodRating);
        Assert.Equal("Yummy!", result.FoodComment);
    }

    [Fact]
    public async Task GetReviewByOrderIdAsync_NonExistingReview_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByOrderIdAsync(999))
            .ReturnsAsync((Review?)null);

        // Act
        var result = await _service.GetReviewByOrderIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region HasReviewForOrderAsync Tests

    [Fact]
    public async Task HasReviewForOrderAsync_ReviewExists_ReturnsTrue()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(42))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasReviewForOrderAsync(42);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasReviewForOrderAsync_ReviewDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.ExistsForOrderAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _service.HasReviewForOrderAsync(999);

        // Assert
        Assert.False(result);
    }

    #endregion
}
