using Microsoft.EntityFrameworkCore;
using MToGo.FeedbackHubService.Entities;
using MToGo.FeedbackHubService.Repositories;

namespace MToGo.FeedbackHubService.Tests.Repositories;

public class ReviewRepositoryTests : IDisposable
{
    private readonly FeedbackHubDbContext _context;
    private readonly ReviewRepository _repository;

    public ReviewRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<FeedbackHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FeedbackHubDbContext(options);
        _repository = new ReviewRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidReview_ReturnsCreatedReview()
    {
        // Arrange
        var review = new Review
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            AgentId = 30,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = "Great food!",
            AgentComment = "Fast delivery",
            OrderComment = "Will order again"
        };

        // Act
        var result = await _repository.CreateAsync(review);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(1, result.OrderId);
        Assert.Equal(10, result.CustomerId);
        Assert.Equal(20, result.PartnerId);
        Assert.Equal(30, result.AgentId);
        Assert.Equal(5, result.FoodRating);
    }

    [Fact]
    public async Task CreateAsync_ReviewPersistsToDatabase()
    {
        // Arrange
        var review = new Review
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        // Act
        await _repository.CreateAsync(review);

        // Assert
        var savedReview = await _context.Reviews.FirstOrDefaultAsync(r => r.OrderId == 1);
        Assert.NotNull(savedReview);
        Assert.Equal(5, savedReview.FoodRating);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var review = new Review
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        // Act
        var result = await _repository.CreateAsync(review);

        // Assert
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CreateAsync_WithNullAgentId_Succeeds()
    {
        // Arrange
        var review = new Review
        {
            OrderId = 1,
            CustomerId = 10,
            PartnerId = 20,
            AgentId = null,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        };

        // Act
        var result = await _repository.CreateAsync(review);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.AgentId);
    }

    [Fact]
    public async Task CreateAsync_WithNullComments_Succeeds()
    {
        // Arrange
        var review = new Review
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

        // Act
        var result = await _repository.CreateAsync(review);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.FoodComment);
        Assert.Null(result.AgentComment);
        Assert.Null(result.OrderComment);
    }

    [Fact]
    public async Task CreateAsync_MultipleReviews_AssignsUniqueIds()
    {
        // Arrange
        var review1 = new Review { OrderId = 1, CustomerId = 10, PartnerId = 20, FoodRating = 5, AgentRating = 4, OrderRating = 5 };
        var review2 = new Review { OrderId = 2, CustomerId = 11, PartnerId = 21, FoodRating = 3, AgentRating = 3, OrderRating = 3 };

        // Act
        var result1 = await _repository.CreateAsync(review1);
        var result2 = await _repository.CreateAsync(review2);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
    }

    #endregion

    #region GetByOrderIdAsync Tests

    [Fact]
    public async Task GetByOrderIdAsync_ExistingReview_ReturnsReview()
    {
        // Arrange
        var review = new Review
        {
            OrderId = 42,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5,
            FoodComment = "Excellent!"
        };
        await _repository.CreateAsync(review);

        // Act
        var result = await _repository.GetByOrderIdAsync(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.OrderId);
        Assert.Equal("Excellent!", result.FoodComment);
    }

    [Fact]
    public async Task GetByOrderIdAsync_NonExistingReview_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByOrderIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrderIdAsync_MultipleReviews_ReturnsCorrectOne()
    {
        // Arrange
        await _repository.CreateAsync(new Review { OrderId = 1, CustomerId = 10, PartnerId = 20, FoodRating = 3, AgentRating = 3, OrderRating = 3, FoodComment = "First" });
        await _repository.CreateAsync(new Review { OrderId = 2, CustomerId = 11, PartnerId = 21, FoodRating = 5, AgentRating = 5, OrderRating = 5, FoodComment = "Second" });
        await _repository.CreateAsync(new Review { OrderId = 3, CustomerId = 12, PartnerId = 22, FoodRating = 1, AgentRating = 1, OrderRating = 1, FoodComment = "Third" });

        // Act
        var result = await _repository.GetByOrderIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.OrderId);
        Assert.Equal("Second", result.FoodComment);
    }

    #endregion

    #region ExistsForOrderAsync Tests

    [Fact]
    public async Task ExistsForOrderAsync_ReviewExists_ReturnsTrue()
    {
        // Arrange
        await _repository.CreateAsync(new Review
        {
            OrderId = 42,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        });

        // Act
        var result = await _repository.ExistsForOrderAsync(42);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsForOrderAsync_ReviewDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsForOrderAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsForOrderAsync_AfterCreatingReview_ReturnsTrue()
    {
        // Arrange
        var orderId = 123;
        Assert.False(await _repository.ExistsForOrderAsync(orderId));

        await _repository.CreateAsync(new Review
        {
            OrderId = orderId,
            CustomerId = 10,
            PartnerId = 20,
            FoodRating = 5,
            AgentRating = 4,
            OrderRating = 5
        });

        // Act
        var result = await _repository.ExistsForOrderAsync(orderId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsForOrderAsync_ChecksOnlySpecificOrder()
    {
        // Arrange
        await _repository.CreateAsync(new Review { OrderId = 1, CustomerId = 10, PartnerId = 20, FoodRating = 5, AgentRating = 4, OrderRating = 5 });
        await _repository.CreateAsync(new Review { OrderId = 2, CustomerId = 11, PartnerId = 21, FoodRating = 5, AgentRating = 4, OrderRating = 5 });

        // Act & Assert
        Assert.True(await _repository.ExistsForOrderAsync(1));
        Assert.True(await _repository.ExistsForOrderAsync(2));
        Assert.False(await _repository.ExistsForOrderAsync(3));
    }

    #endregion
}

