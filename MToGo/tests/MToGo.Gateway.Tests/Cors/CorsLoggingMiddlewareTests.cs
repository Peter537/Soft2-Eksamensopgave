using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MToGo.Shared.Security.Cors;
using Moq;
using System.Net;

namespace MToGo.Gateway.Tests.Cors;

/// <summary>
/// Tests for the CORS logging middleware that logs blocked origin requests.
/// </summary>
public class CorsLoggingMiddlewareTests
{
    private const string AllowedOrigin = "http://localhost:8081";
    private const string BlockedOrigin = "http://evil-site.com";
    private const string TestEndpoint = "/api/test";

    private static IHost CreateTestServerWithLogging(CorsSettings corsSettings, Mock<ILogger<CorsLoggingMiddleware>> mockLogger)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(corsSettings);
                    services.AddSingleton(mockLogger.Object);
                    services.AddRouting();
                    services.AddCors(options =>
                    {
                        options.AddPolicy(CorsPolicies.TrustedOrigins, builder =>
                        {
                            if (corsSettings.AllowedOrigins.Length > 0)
                            {
                                builder.WithOrigins(corsSettings.AllowedOrigins);
                            }
                            builder.AllowAnyMethod().AllowAnyHeader();
                            if (corsSettings.AllowCredentials)
                            {
                                builder.AllowCredentials();
                            }
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    // Add CORS logging middleware before CORS
                    app.UseCorsLogging(corsSettings);
                    app.UseCors(CorsPolicies.TrustedOrigins);
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(TestEndpoint, () => Results.Ok("test"));
                    });
                });
            })
            .Build();
    }

    [Fact]
    public async Task BlockedOrigin_WithLoggingEnabled_LogsWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CorsLoggingMiddleware>>();
        var settings = new CorsSettings
        {
            AllowedOrigins = new[] { AllowedOrigin },
            AllowCredentials = true,
            LogBlockedRequests = true
        };

        using var host = CreateTestServerWithLogging(settings, mockLogger);
        await host.StartAsync();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("Origin", BlockedOrigin);

        // Act
        await client.SendAsync(request);

        // Assert - verify that a warning was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Blocked request from non-compliant origin")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AllowedOrigin_DoesNotLogWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CorsLoggingMiddleware>>();
        var settings = new CorsSettings
        {
            AllowedOrigins = new[] { AllowedOrigin },
            AllowCredentials = true,
            LogBlockedRequests = true
        };

        using var host = CreateTestServerWithLogging(settings, mockLogger);
        await host.StartAsync();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("Origin", AllowedOrigin);

        // Act
        await client.SendAsync(request);

        // Assert - verify that NO warning was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Blocked")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task BlockedOrigin_WithLoggingDisabled_DoesNotLogWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CorsLoggingMiddleware>>();
        var settings = new CorsSettings
        {
            AllowedOrigins = new[] { AllowedOrigin },
            AllowCredentials = true,
            LogBlockedRequests = false // Logging disabled
        };

        using var host = CreateTestServerWithLogging(settings, mockLogger);
        await host.StartAsync();
        var client = host.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("Origin", BlockedOrigin);

        // Act
        await client.SendAsync(request);

        // Assert - verify that NO warning was logged (logging is disabled)
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestWithoutOriginHeader_PassesThrough()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<CorsLoggingMiddleware>>();
        var settings = new CorsSettings
        {
            AllowedOrigins = new[] { AllowedOrigin },
            AllowCredentials = true,
            LogBlockedRequests = true
        };

        using var host = CreateTestServerWithLogging(settings, mockLogger);
        await host.StartAsync();
        var client = host.GetTestClient();

        // Request without Origin header (same-origin or non-browser request)
        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // No CORS headers should be present for requests without Origin
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        // No warnings should be logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
