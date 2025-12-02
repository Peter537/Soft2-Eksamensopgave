using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MToGo.PartnerService.Data;
using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Tests.Fixtures;
using MToGo.Testing;

namespace MToGo.PartnerService.Tests.Integration;

[Collection("PartnerService Integration Tests")]
public class MenuItemIntegrationTests : IAsyncLifetime
{
    private readonly PartnerServiceTestFixture _fixture;
    private PartnerServiceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public MenuItemIntegrationTests(PartnerServiceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PartnerServiceWebApplicationFactory(_fixture);
        await _factory.InitializeDatabaseAsync();
        await _factory.CleanupDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        TestAuthenticationHandler.ClearTestUser();
        await _factory.DisposeAsync();
    }

    #region Add Menu Item Tests

    [Fact]
    public async Task AddMenuItem_WithValidRequest_Returns201Created()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new CreateMenuItemRequest
        {
            Name = "New Burger",
            Price = 99.50m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateMenuItemResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddMenuItem_WithEmptyName_Returns400BadRequest()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new CreateMenuItemRequest
        {
            Name = "",
            Price = 50.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMenuItem_WithZeroPrice_Returns400BadRequest()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new CreateMenuItemRequest
        {
            Name = "Free Item",
            Price = 0m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMenuItem_WithNegativePrice_Returns400BadRequest()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new CreateMenuItemRequest
        {
            Name = "Negative Item",
            Price = -10.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMenuItem_ToAnotherPartnersAccount_Returns403Forbidden()
    {
        // Arrange
        var partnerId1 = await CreateTestPartnerAsync("Partner 1", "partner1@test.com");
        var partnerId2 = await CreateTestPartnerAsync("Partner 2", "partner2@test.com");
        TestAuthenticationHandler.SetTestUser(partnerId1.ToString(), "Partner");

        var request = new CreateMenuItemRequest
        {
            Name = "New Burger",
            Price = 99.50m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/partners/{partnerId2}/menu/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddMenuItem_PersistsCorrectly()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new CreateMenuItemRequest
        {
            Name = "Pizza",
            Price = 120.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items", request);
        var result = await response.Content.ReadFromJsonAsync<CreateMenuItemResponse>();

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
        var menuItem = await dbContext.MenuItems.FindAsync(result!.Id);

        menuItem.Should().NotBeNull();
        menuItem!.Name.Should().Be("Pizza");
        menuItem.Price.Should().Be(120.00m);
        menuItem.PartnerId.Should().Be(partnerId);
        menuItem.IsActive.Should().BeTrue();
    }

    #endregion

    #region Update Menu Item Tests

    [Fact]
    public async Task UpdateMenuItem_WithNameOnly_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        var menuItemId = await CreateTestMenuItemAsync(partnerId, "Old Name", 50.00m);
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new UpdateMenuItemRequest
        {
            Name = "New Name"
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items/{menuItemId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
        var menuItem = await dbContext.MenuItems.FindAsync(menuItemId);

        menuItem!.Name.Should().Be("New Name");
        menuItem.Price.Should().Be(50.00m); // Unchanged
    }

    [Fact]
    public async Task UpdateMenuItem_WithPriceOnly_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        var menuItemId = await CreateTestMenuItemAsync(partnerId, "Burger", 50.00m);
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new UpdateMenuItemRequest
        {
            Price = 75.00m
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items/{menuItemId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
        var menuItem = await dbContext.MenuItems.FindAsync(menuItemId);

        menuItem!.Name.Should().Be("Burger"); // Unchanged
        menuItem.Price.Should().Be(75.00m);
    }

    [Fact]
    public async Task UpdateMenuItem_WithNameAndPrice_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        var menuItemId = await CreateTestMenuItemAsync(partnerId, "Old Burger", 50.00m);
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        var request = new UpdateMenuItemRequest
        {
            Name = "New Burger",
            Price = 85.00m
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/partners/{partnerId}/menu/items/{menuItemId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
        var menuItem = await dbContext.MenuItems.FindAsync(menuItemId);

        menuItem!.Name.Should().Be("New Burger");
        menuItem.Price.Should().Be(85.00m);
    }

    [Fact]
    public async Task UpdateMenuItem_AnotherPartnersItem_Returns403Forbidden()
    {
        // Arrange
        var partnerId1 = await CreateTestPartnerAsync("Partner 1", "partner1@test.com");
        var partnerId2 = await CreateTestPartnerAsync("Partner 2", "partner2@test.com");
        var menuItemId = await CreateTestMenuItemAsync(partnerId2, "Burger", 50.00m);
        TestAuthenticationHandler.SetTestUser(partnerId1.ToString(), "Partner");

        var request = new UpdateMenuItemRequest
        {
            Name = "Hacked Burger"
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/v1/partners/{partnerId2}/menu/items/{menuItemId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Delete Menu Item Tests

    [Fact]
    public async Task DeleteMenuItem_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        var menuItemId = await CreateTestMenuItemAsync(partnerId, "To Delete", 10.00m);
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        // Act
        var response = await _client.DeleteAsync($"/api/v1/partners/{partnerId}/menu/items/{menuItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
        var menuItem = await dbContext.MenuItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == menuItemId);

        menuItem.Should().NotBeNull();
        menuItem!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMenuItem_NonExistent_Returns404NotFound()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        // Act
        var response = await _client.DeleteAsync($"/api/v1/partners/{partnerId}/menu/items/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMenuItem_AnotherPartnersItem_Returns403Forbidden()
    {
        // Arrange
        var partnerId1 = await CreateTestPartnerAsync("Partner 1", "partner1@test.com");
        var partnerId2 = await CreateTestPartnerAsync("Partner 2", "partner2@test.com");
        var menuItemId = await CreateTestMenuItemAsync(partnerId2, "Burger", 50.00m);
        TestAuthenticationHandler.SetTestUser(partnerId1.ToString(), "Partner");

        // Act
        var response = await _client.DeleteAsync($"/api/v1/partners/{partnerId2}/menu/items/{menuItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Get Partner Details Tests

    [Fact]
    public async Task GetPartnerDetails_WithValidPartner_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com");
        await CreateTestMenuItemAsync(partnerId, "Margherita", 89.00m);
        await CreateTestMenuItemAsync(partnerId, "Pepperoni", 99.00m);
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        // Act
        var response = await _client.GetAsync($"/api/v1/partners/{partnerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PartnerDetailsResponse>();

        result.Should().NotBeNull();
        result!.Id.Should().Be(partnerId);
        result.Name.Should().Be("Pizza Palace");
        result.MenuItems.Should().HaveCount(3); // 1 default + 2 added
    }

    [Fact]
    public async Task GetPartnerDetails_NonExistent_Returns404NotFound()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync();
        TestAuthenticationHandler.SetTestUser(partnerId.ToString(), "Partner");

        // Act
        var response = await _client.GetAsync("/api/v1/partners/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task<int> CreateTestPartnerAsync(string name = "Test Partner", string email = "test@example.com")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();

        var partner = new Partner
        {
            Name = name,
            Address = "123 Test Street",
            Email = email,
            Password = "$2a$12$hashedpassword",
            IsActive = true,
            MenuItems = new List<MenuItem>
            {
                new MenuItem { Name = "Default Item", Price = 10.00m, IsActive = true }
            }
        };

        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        return partner.Id;
    }

    private async Task<int> CreateTestMenuItemAsync(int partnerId, string name, decimal price)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();

        var menuItem = new MenuItem
        {
            PartnerId = partnerId,
            Name = name,
            Price = price,
            IsActive = true
        };

        dbContext.MenuItems.Add(menuItem);
        await dbContext.SaveChangesAsync();

        return menuItem.Id;
    }

    #endregion
}
