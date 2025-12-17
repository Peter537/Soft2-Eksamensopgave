using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MToGo.PartnerService.Entities;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Tests.Fixtures;

namespace MToGo.PartnerService.Tests.Integration;

[Collection("PartnerService Integration Tests")]
public class PublicBrowsingIntegrationTests : IAsyncLifetime
{
    private readonly PartnerServiceTestFixture _fixture;
    private PartnerServiceWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public PublicBrowsingIntegrationTests(PartnerServiceTestFixture fixture)
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
        await _factory.DisposeAsync();
    }

    #region GetAllPartners Tests

    [Fact]
    public async Task GetAllPartners_WithActivePartners_Returns200Ok()
    {
        // Arrange
        await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com", true);
        await CreateTestPartnerAsync("Burger Joint", "burger@test.com", true);

        // Act
        var response = await _client.GetAsync("/api/v1/partners");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PublicPartnerResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllPartners_WithNoPartners_ReturnsEmptyList()
    {
        // Arrange - no partners created

        // Act
        var response = await _client.GetAsync("/api/v1/partners");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PublicPartnerResponse>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPartners_ExcludesInactivePartners()
    {
        // Arrange
        await CreateTestPartnerAsync("Active Partner", "active@test.com", true);
        await CreateTestPartnerAsync("Inactive Partner", "inactive@test.com", false);

        // Act
        var response = await _client.GetAsync("/api/v1/partners");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PublicPartnerResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Partner");
    }

    [Fact]
    public async Task GetAllPartners_ReturnsCorrectPartnerData()
    {
        // Arrange
        await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com", true);

        // Act
        var response = await _client.GetAsync("/api/v1/partners");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PublicPartnerResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result[0].Name.Should().Be("Pizza Palace");
        result[0].Address.Should().Be("123 Test Street");
    }

    #endregion

    #region GetPartnerMenu Tests

    [Fact]
    public async Task GetPartnerMenu_WithExistingPartner_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com", true);
        await CreateTestMenuItemAsync(partnerId, "Margherita", 89.00m);
        await CreateTestMenuItemAsync(partnerId, "Pepperoni", 99.00m);

        // Act
        var response = await _client.GetAsync($"/api/v1/partners/{partnerId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublicMenuResponse>();
        result.Should().NotBeNull();
        result!.PartnerId.Should().Be(partnerId);
        result.PartnerName.Should().Be("Pizza Palace");
        result.IsActive.Should().BeTrue();
        result.Items.Should().HaveCount(3); // 1 default + 2 added
    }

    [Fact]
    public async Task GetPartnerMenu_WithNonExistentPartner_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/partners/999/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPartnerMenu_WithInactivePartner_ReturnsMenuWithIsActiveFalse()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync("Inactive Partner", "inactive@test.com", false);

        // Act
        var response = await _client.GetAsync($"/api/v1/partners/{partnerId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublicMenuResponse>();
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetPartnerMenu_ReturnsMenuItemsCorrectly()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com", true);
        await CreateTestMenuItemAsync(partnerId, "Special Pizza", 150.00m);

        // Act
        var response = await _client.GetAsync($"/api/v1/partners/{partnerId}/menu");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublicMenuResponse>();
        result.Should().NotBeNull();
        
        var specialPizza = result!.Items.FirstOrDefault(i => i.Name == "Special Pizza");
        specialPizza.Should().NotBeNull();
        specialPizza!.Price.Should().Be(150.00m);
    }

    #endregion

    #region GetMenuItem Tests

    [Fact]
    public async Task GetMenuItem_WithExistingItem_Returns200Ok()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com", true);
        var menuItemId = await CreateTestMenuItemAsync(partnerId, "Special Pizza", 150.00m);

        // Act
        var response = await _client.GetAsync($"/api/v1/partners/{partnerId}/menu/items/{menuItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublicMenuItemResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(menuItemId);
        result.Name.Should().Be("Special Pizza");
        result.Price.Should().Be(150.00m);
    }

    [Fact]
    public async Task GetMenuItem_WithNonExistentPartner_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/partners/999/menu/items/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMenuItem_WithNonExistentMenuItem_Returns404NotFound()
    {
        // Arrange
        var partnerId = await CreateTestPartnerAsync("Pizza Palace", "pizza@test.com", true);

        // Act
        var response = await _client.GetAsync($"/api/v1/partners/{partnerId}/menu/items/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMenuItem_WithItemFromDifferentPartner_Returns404NotFound()
    {
        // Arrange
        var partner1Id = await CreateTestPartnerAsync("Partner 1", "partner1@test.com", true);
        var partner2Id = await CreateTestPartnerAsync("Partner 2", "partner2@test.com", true);
        var menuItemId = await CreateTestMenuItemAsync(partner2Id, "Partner 2 Item", 50.00m);

        // Act - try to get partner2's item through partner1's menu
        var response = await _client.GetAsync($"/api/v1/partners/{partner1Id}/menu/items/{menuItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private async Task<int> CreateTestPartnerAsync(string name, string email, bool isActive)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();

        var partner = new Partner
        {
            Name = name,
            Address = "123 Test Street",
            Email = email,
            Password = "$2a$12$hashedpassword",
            IsActive = isActive,
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
