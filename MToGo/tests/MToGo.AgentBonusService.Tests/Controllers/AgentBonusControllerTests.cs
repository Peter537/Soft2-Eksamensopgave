using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.AgentBonusService.Controllers;
using MToGo.AgentBonusService.Models;
using MToGo.AgentBonusService.Services;

namespace MToGo.AgentBonusService.Tests.Controllers;

public class AgentBonusControllerTests
{
    private readonly Mock<IAgentBonusService> _bonusServiceMock;
    private readonly Mock<ILogger<AgentBonusController>> _loggerMock;
    private readonly AgentBonusController _target;
    private readonly DefaultHttpContext _httpContext;

    public AgentBonusControllerTests()
    {
        _bonusServiceMock = new Mock<IAgentBonusService>();
        _loggerMock = new Mock<ILogger<AgentBonusController>>();
        _target = new AgentBonusController(_bonusServiceMock.Object, _loggerMock.Object);
        
        _httpContext = new DefaultHttpContext();
        _target.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    #region CalculateBonus Tests

    [Fact]
    public async Task CalculateBonus_WithValidRequest_ReturnsOkWithBonusResult()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var expectedResult = CreateQualifiedBonusResult(agentId, 87.00m);

        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var bonusResult = okResult.Value.Should().BeOfType<BonusCalculationResponse>().Subject;
        bonusResult.AgentId.Should().Be(agentId);
        bonusResult.BonusAmount.Should().Be(87.00m);
    }

    [Fact]
    public async Task CalculateBonus_ExtractsAuthTokenFromHeader()
    {
        // Arrange
        var agentId = 1;
        var authHeader = "Bearer test-token-123";
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        _httpContext.Request.Headers["Authorization"] = authHeader;

        string? capturedToken = null;
        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .Callback<int, DateTime, DateTime, string?>((_, _, _, token) => capturedToken = token)
            .ReturnsAsync(CreateQualifiedBonusResult(agentId, 87.00m));

        // Act
        await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        // GetAuthToken strips "Bearer " prefix
        capturedToken.Should().Be("test-token-123");
    }

    [Fact]
    public async Task CalculateBonus_WhenNotQualified_ReturnsOkWithDisqualifiedResult()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var notQualifiedResult = new BonusCalculationResponse
        {
            AgentId = agentId,
            AgentName = "Test Agent",
            Qualified = false,
            DisqualificationReason = "Minimum 20 deliveries required (has 5)",
            DeliveryCount = 5,
            BonusAmount = 0
        };

        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .ReturnsAsync(notQualifiedResult);

        // Act
        var result = await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var bonusResult = okResult.Value.Should().BeOfType<BonusCalculationResponse>().Subject;
        bonusResult.Qualified.Should().BeFalse();
        bonusResult.DisqualificationReason.Should().Contain("20 deliveries");
    }

    [Fact]
    public async Task CalculateBonus_PassesCorrectDateRange()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 11, 1);
        var endDate = new DateTime(2025, 11, 30);

        DateTime capturedStart = default;
        DateTime capturedEnd = default;

        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .Callback<int, DateTime, DateTime, string?>((_, start, end, _) =>
            {
                capturedStart = start;
                capturedEnd = end;
            })
            .ReturnsAsync(CreateQualifiedBonusResult(agentId, 50.00m));

        // Act
        await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        capturedStart.Should().Be(startDate);
        capturedEnd.Should().Be(endDate);
    }

    [Fact]
    public async Task CalculateBonus_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 31);
        var endDate = new DateTime(2025, 12, 1); // Before start date

        // Act
        var result = await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CalculateBonus_WithDateRangeOver1Year_ReturnsBadRequest()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2025, 12, 31); // More than 1 year

        // Act
        var result = await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetBonusPreview Tests

    [Fact]
    public async Task GetBonusPreview_WithValidDates_ReturnsOk()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 6, 1);
        var endDate = new DateTime(2025, 6, 30);

        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .ReturnsAsync(CreateQualifiedBonusResult(agentId, 75.00m));

        // Act
        var result = await _target.GetBonusPreview(agentId, startDate, endDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBonusPreview_ExtractsAuthTokenFromHeader()
    {
        // Arrange
        var agentId = 1;
        var authHeader = "Bearer preview-token";
        var startDate = new DateTime(2025, 6, 1);
        var endDate = new DateTime(2025, 6, 30);

        _httpContext.Request.Headers["Authorization"] = authHeader;

        string? capturedToken = null;
        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .Callback<int, DateTime, DateTime, string?>((_, _, _, token) => capturedToken = token)
            .ReturnsAsync(CreateQualifiedBonusResult(agentId, 50.00m));

        // Act
        await _target.GetBonusPreview(agentId, startDate, endDate);

        // Assert
        capturedToken.Should().Be("preview-token");
    }

    [Fact]
    public async Task GetBonusPreview_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 31);
        var endDate = new DateTime(2025, 12, 1); // Before start date

        // Act
        var result = await _target.GetBonusPreview(agentId, startDate, endDate);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CalculateBonus_IncludesWarningsInResult()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        var resultWithWarnings = CreateQualifiedBonusResult(agentId, 50.00m);
        resultWithWarnings.UsedDefaultRating = true;
        resultWithWarnings.Warnings.Add("Feedback Hub unavailable, using default rating");

        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .ReturnsAsync(resultWithWarnings);

        // Act
        var result = await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var bonusResult = okResult.Value.Should().BeOfType<BonusCalculationResponse>().Subject;
        bonusResult.UsedDefaultRating.Should().BeTrue();
        bonusResult.Warnings.Should().Contain(w => w.Contains("Feedback Hub"));
    }

    [Fact]
    public async Task CalculateBonus_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var agentId = 1;
        var startDate = new DateTime(2025, 12, 1);
        var endDate = new DateTime(2025, 12, 31);

        _bonusServiceMock
            .Setup(x => x.CalculateBonusAsync(agentId, startDate, endDate, It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _target.CalculateBonus(agentId, startDate, endDate);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Health_ReturnsOk()
    {
        // Act
        var result = _target.Health();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region Helper Methods

    private BonusCalculationResponse CreateQualifiedBonusResult(int agentId, decimal bonusAmount)
    {
        return new BonusCalculationResponse
        {
            AgentId = agentId,
            AgentName = "Test Agent",
            Qualified = true,
            DeliveryCount = 25,
            TotalDeliveryFees = bonusAmount / 0.15m, // Reverse calculate
            Contribution = bonusAmount,
            TimeScore = 1.0m,
            ReviewScore = 1.0m,
            Performance = 1.0m,
            BonusAmount = bonusAmount,
            EarlyDeliveries = 25,
            NormalDeliveries = 0,
            LateDeliveries = 0,
            ReviewCount = 10,
            AverageRating = 5.0m,
            UsedDefaultRating = false,
            Period = new BonusPeriod
            {
                StartDate = new DateTime(2025, 12, 1),
                EndDate = new DateTime(2025, 12, 31)
            }
        };
    }

    #endregion
}

