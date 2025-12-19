using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using MToGo.Shared.Security.Authentication;

namespace MToGo.Performance.Tests;

/// <summary>
/// Performance tests using NBomber.
/// 
/// Run with: dotnet test --filter "Category=Performance" -e RUN_PERFORMANCE_TESTS=true
/// 
/// Or set environment variable RUN_PERFORMANCE_TESTS=true and just run: dotnet test
/// </summary>
[Trait("Category", "Performance")]
public class PerformanceTests
{
    private const string GatewayUrl = "http://localhost:8080";
    private readonly HttpClient _httpClient = new();
    
    // JWT service for token generation
    private static readonly IJwtTokenService _jwtService = new JwtTokenService(Options.Create(new JwtSettings
    {
        SecretKey = JwtSecretKey,
        Issuer = JwtIssuer,
        Audience = JwtAudience,
        ExpirationMinutes = 60
    }));
    
    // Check if performance tests should run (set RUN_PERFORMANCE_TESTS=true)
    private static bool ShouldRunPerformanceTests => 
        Environment.GetEnvironmentVariable("RUN_PERFORMANCE_TESTS")?.ToLower() == "true";

    // ============================================================
    // TRAFFIC CALCULATIONS (from our queueing theory)
    // ============================================================
    
    // Business target: 18 million orders/year
    private const int YearlyOrders = 18_000_000;
    
    // Peak hours (lunch + dinner) are 3x busier
    private const double PeakMultiplier = 3.0;
    private const int PeakHours = 7;
    
    /// <summary>
    /// Calculate peak requests per second based on business requirements.
    /// 
    /// Formula: (Daily orders × Peak multiplier) / (Peak hours × 3600)
    /// = (49,315 × 3) / (7 × 3600) ≈ 5.9 req/s
    /// </summary>
    private static double CalculatePeakRequestsPerSecond()
    {
        double dailyOrders = (double)YearlyOrders / 365;
        double peakOrders = dailyOrders * PeakMultiplier;
        return peakOrders / (PeakHours * 3600);
    }
    
    /// <summary>
    /// Calculate system utilization.
    /// Keep under 70-80% for stable performance!
    /// </summary>
    private static double CalculateUtilization(double arrivalRate, double serviceRate)
        => (arrivalRate / serviceRate) * 100;

    // ============================================================
    // PERFORMANCE TESTS
    // ============================================================

    [SkippableFact]
    public void SmokeTest_SystemResponds()
    {
        Skip.IfNot(ShouldRunPerformanceTests, "Set RUN_PERFORMANCE_TESTS=true to run");
        
        // Using 1 req/s - minimal load just to verify system works
        var scenario = Scenario.Create("smoke_test", async context =>
        {
            var request = Http.CreateRequest("GET", $"{GatewayUrl}/health");
            return await Http.Send(_httpClient, request);
        })
        .WithoutWarmUp()
        .WithLoadSimulations(
            Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports/smoke")
            .Run();

        var stats = result.ScenarioStats[0];
        Assert.Equal(0, stats.Fail.Request.Count);
    }

    [SkippableFact]
    public void LoadTest_NormalTraffic()
    {
        Skip.IfNot(ShouldRunPerformanceTests, "Set RUN_PERFORMANCE_TESTS=true to run");
        
        // Use our calculated peak rate (~6 req/s)
        var peakRate = (int)Math.Ceiling(CalculatePeakRequestsPerSecond());
        Console.WriteLine($"Testing with peak rate: {peakRate} req/s (calculated from 18M orders/year)");
        
        var scenario = Scenario.Create("load_test", async context =>
        {
            // Use pre-generated customer identities to better simulate real clients
            var identity = GetRandomCustomerIdentity();
            
            // Hit the customer orders endpoint - tests read operations with auth
            var request = Http.CreateRequest("GET", $"{GatewayUrl}/api/v1/orders/customer/{identity.CustomerId}")
                .WithHeader("Authorization", $"Bearer {identity.Token}");
            return await Http.Send(_httpClient, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(30))
        .WithLoadSimulations(
            Simulation.RampingInject(rate: peakRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: peakRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
        );

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports/load")
            .Run();

        var stats = result.ScenarioStats[0];
        
        // Check utilization estimate (assuming ~10 req/s service capacity)
        var utilization = CalculateUtilization(peakRate, 10);
        Console.WriteLine($"Estimated utilization at {peakRate} req/s: {utilization}%");
        
        var errorRate = (double)stats.Fail.Request.Count / stats.AllRequestCount * 100;
        Assert.True(errorRate < 1, $"Error rate {errorRate:F2}% exceeds 1%");
        Assert.True(stats.Ok.Latency.Percent95 < 500, $"P95 latency {stats.Ok.Latency.Percent95}ms exceeds 500ms");
        Assert.True(stats.Ok.Latency.Percent99 < 1000, $"P99 latency {stats.Ok.Latency.Percent99}ms exceeds 1000ms");
    }

    [SkippableFact]
    public void StressTest_FindBreakingPoint()
    {
        Skip.IfNot(ShouldRunPerformanceTests, "Set RUN_PERFORMANCE_TESTS=true to run");
        
        var peakRate = (int)Math.Ceiling(CalculatePeakRequestsPerSecond());
        
        // Stress test: go from peak to 5x peak to find breaking point
        Console.WriteLine($"Stress test: {peakRate} -> {peakRate * 2} -> {peakRate * 5} req/s");
        
        var scenario = Scenario.Create("stress_test", async context =>
        {
            var identity = GetRandomCustomerIdentity();
            
            // Hit the customer orders endpoint for stress testing read operations
            var request = Http.CreateRequest("GET", $"{GatewayUrl}/api/v1/orders/customer/{identity.CustomerId}")
                .WithHeader("Authorization", $"Bearer {identity.Token}");
            return await Http.Send(_httpClient, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: peakRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: peakRate * 2, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: peakRate * 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports/stress")
            .Run();

        var stats = result.ScenarioStats[0];
        Console.WriteLine($"Total: {stats.AllRequestCount}, Failed: {stats.Fail.Request.Count}");
        Console.WriteLine($"Error rate: {(double)stats.Fail.Request.Count / stats.AllRequestCount * 100:F2}%");
        Console.WriteLine($"P95 latency: {stats.Ok.Latency.Percent95}ms");
    }

    // ============================================================
    // ORDER FLOW TEST (requires full stack running)
    // ============================================================

    // JWT settings matching the services' appsettings.json
    private const string JwtSecretKey = "MToGo-Super-Secret-Key-That-Should-Be-At-Least-256-Bits-Long-For-Security!";
    private const string JwtIssuer = "MToGo";
    private const string JwtAudience = "MToGo-Services";

    // Pre-generated customer identities to avoid generating JWTs per request
    private const int CustomerTokenPoolSize = 1000;

    private static readonly (int CustomerId, string Token)[] CustomerIdentities =
        Enumerable.Range(1, CustomerTokenPoolSize)
            .Select(id => (CustomerId: id, Token: GenerateTestToken(id)))
            .ToArray();

    private static (int CustomerId, string Token) GetRandomCustomerIdentity()
        => CustomerIdentities[Random.Shared.Next(CustomerIdentities.Length)];

    /// <summary>
    /// Generate a valid JWT token for testing.
    /// </summary>
    private static string GenerateTestToken(int customerId = 1)
    {
        return _jwtService.GenerateToken(customerId, $"{customerId}@test.com", "Customer");
    }

    [SkippableFact]
    public void LoadTest_OrderFlow()
    {
        Skip.IfNot(ShouldRunPerformanceTests, "Set RUN_PERFORMANCE_TESTS=true to run");
        
        // Order creation at half peak rate (orders are heavier than reads)
        var orderRate = (int)Math.Ceiling(CalculatePeakRequestsPerSecond() / 2);
        Console.WriteLine($"Order flow test at {orderRate} req/s");
        
        var scenario = Scenario.Create("order_flow", async context =>
        {
            var identity = GetRandomCustomerIdentity();
            
            // Vary the order data to simulate real traffic
            var itemCount = Random.Shared.Next(1, 5);
            var items = new List<string>();
            for (int i = 0; i < itemCount; i++)
            {
                var foodItemId = Random.Shared.Next(1, 50);
                var quantity = Random.Shared.Next(1, 4);
                var unitPrice = Random.Shared.Next(50, 200);
                items.Add($$"""{"foodItemId": {{foodItemId}}, "name": "Test Item {{foodItemId}}", "quantity": {{quantity}}, "unitPrice": {{unitPrice}}.00}""");
            }
            
            var orderPayload = $$"""
            {
                "customerId": {{identity.CustomerId}},
                "partnerId": {{Random.Shared.Next(1, 10)}},
                "deliveryAddress": "Test St {{Random.Shared.Next(1, 100)}}, Copenhagen, 2100",
                "deliveryFee": {{Random.Shared.Next(25, 50)}}.00,
                "distance": "{{Random.Shared.Next(2, 10)}}.{{Random.Shared.Next(0, 10)}}km",
                "items": [{{string.Join(",", items)}}]
            }
            """;
            
            var request = Http.CreateRequest("POST", $"{GatewayUrl}/api/v1/orders/order")
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Authorization", $"Bearer {identity.Token}")
                .WithBody(new StringContent(orderPayload, Encoding.UTF8, "application/json"));
            
            return await Http.Send(_httpClient, request);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(30))
        .WithLoadSimulations(
            Simulation.Inject(rate: orderRate, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
        );

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("reports/order-flow")
            .Run();

        var stats = result.ScenarioStats[0];
        Console.WriteLine($"Orders attempted: {stats.AllRequestCount}");
        Console.WriteLine($"Successful: {stats.Ok.Request.Count}");
        Console.WriteLine($"Failed: {stats.Fail.Request.Count}");
        
        var errorRate = (double)stats.Fail.Request.Count / stats.AllRequestCount * 100;
        Assert.True(errorRate < 5, $"Error rate {errorRate:F2}% exceeds 5%");
        Assert.True(stats.Ok.Latency.Percent95 < 1000, $"P95 latency {stats.Ok.Latency.Percent95}ms exceeds 1000ms");
        Assert.True(stats.Ok.Latency.Percent99 < 2000, $"P99 latency {stats.Ok.Latency.Percent99}ms exceeds 2000ms");
    }
}
