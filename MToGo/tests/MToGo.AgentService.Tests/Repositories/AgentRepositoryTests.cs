using Microsoft.EntityFrameworkCore;
using MToGo.AgentService.Data;
using MToGo.AgentService.Entities;
using MToGo.AgentService.Repositories;

namespace MToGo.AgentService.Tests.Repositories;

public class AgentRepositoryTests : IDisposable
{
    private readonly AgentDbContext _context;
    private readonly AgentRepository _sut;

    public AgentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AgentDbContext(options);
        _sut = new AgentRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsAgent()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "John Agent",
            Email = "john@example.com",
            Password = "hashed-password",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(agent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agent.Id, result.Id);
        Assert.Equal("John Agent", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedAgent_ReturnsNull()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Deleted Agent",
            Email = "deleted@example.com",
            Password = "hashed",
            IsActive = true,
            IsDeleted = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(agent.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsAgent()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "John Agent",
            Email = "john@example.com",
            Password = "hashed-password",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEmailAsync("john@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_WithDeletedAgent_ReturnsNull()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Deleted Agent",
            Email = "deleted@example.com",
            Password = "hashed",
            IsActive = true,
            IsDeleted = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEmailAsync("deleted@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseSensitive()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "John Agent",
            Email = "John@Example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEmailAsync("john@example.com");

        // Assert - Email comparison is case-sensitive by default in EF
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidAgent_ReturnsAgentWithId()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "New Agent",
            Email = "new@example.com",
            Password = "hashed-password",
            IsActive = true
        };

        // Act
        var result = await _sut.CreateAsync(agent);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("New Agent", result.Name);
    }

    [Fact]
    public async Task CreateAsync_PersistsAgentToDatabase()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Persistent Agent",
            Email = "persistent@example.com",
            Password = "hashed",
            IsActive = true
        };

        // Act
        var created = await _sut.CreateAsync(agent);

        // Assert
        var retrieved = await _context.Agents.FindAsync(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Persistent Agent", retrieved.Name);
        Assert.Equal("persistent@example.com", retrieved.Email);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
        var agent = new Agent
        {
            Name = "Timestamped Agent",
            Email = "timestamp@example.com",
            Password = "hashed",
            IsActive = true
        };

        // Act
        var result = await _sut.CreateAsync(agent);
        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(result.CreatedAt >= beforeCreate);
        Assert.True(result.CreatedAt <= afterCreate);
    }

    [Fact]
    public async Task CreateAsync_MultipleAgents_AssignsUniqueIds()
    {
        // Arrange
        var agent1 = new Agent { Name = "Agent 1", Email = "agent1@example.com", Password = "hashed" };
        var agent2 = new Agent { Name = "Agent 2", Email = "agent2@example.com", Password = "hashed" };
        var agent3 = new Agent { Name = "Agent 3", Email = "agent3@example.com", Password = "hashed" };

        // Act
        var result1 = await _sut.CreateAsync(agent1);
        var result2 = await _sut.CreateAsync(agent2);
        var result3 = await _sut.CreateAsync(agent3);

        // Assert
        var ids = new[] { result1.Id, result2.Id, result3.Id };
        Assert.Equal(ids.Distinct().Count(), ids.Length);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidAgent_UpdatesAgent()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Original Name",
            Email = "original@example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        agent.Name = "Updated Name";
        var result = await _sut.UpdateAsync(agent);

        // Assert
        Assert.Equal("Updated Name", result.Name);
        var retrieved = await _context.Agents.FindAsync(agent.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Agent",
            Email = "agent@example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);

        // Act
        agent.Name = "Updated Agent";
        var result = await _sut.UpdateAsync(agent);
        var afterUpdate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt >= beforeUpdate);
        Assert.True(result.UpdatedAt <= afterUpdate);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_SoftDeletesAgent()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "To Delete",
            Email = "delete@example.com",
            Password = "hashed",
            IsActive = true,
            IsDeleted = false
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(agent.Id);

        // Assert
        var retrieved = await _context.Agents.FindAsync(agent.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "To Delete",
            Email = "delete@example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        var beforeDelete = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await _sut.DeleteAsync(agent.Id);
        var afterDelete = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var retrieved = await _context.Agents.FindAsync(agent.Id);
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.UpdatedAt);
        Assert.True(retrieved.UpdatedAt >= beforeDelete);
        Assert.True(retrieved.UpdatedAt <= afterDelete);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _sut.DeleteAsync(999);
    }

    [Fact]
    public async Task DeleteAsync_DeletedAgentNotReturnedByGetById()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "To Delete",
            Email = "delete@example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(agent.Id);
        var result = await _sut.GetByIdAsync(agent.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Agent",
            Email = "existing@example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.EmailExistsAsync("existing@example.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistingEmail_ReturnsFalse()
    {
        // Act
        var result = await _sut.EmailExistsAsync("nonexistent@example.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EmailExistsAsync_WithDeletedAgent_ReturnsFalse()
    {
        // Arrange
        var agent = new Agent
        {
            Name = "Deleted Agent",
            Email = "deleted@example.com",
            Password = "hashed",
            IsActive = true,
            IsDeleted = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.EmailExistsAsync("deleted@example.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task EmailExistsAsync_AllowsReuseOfDeletedEmail()
    {
        // Arrange - Create and delete an agent
        var agent = new Agent
        {
            Name = "Original Agent",
            Email = "reuse@example.com",
            Password = "hashed",
            IsActive = true
        };
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();
        await _sut.DeleteAsync(agent.Id);

        // Act - Check if email can be reused
        var emailExists = await _sut.EmailExistsAsync("reuse@example.com");

        // Assert
        Assert.False(emailExists);
    }

    #endregion
}
