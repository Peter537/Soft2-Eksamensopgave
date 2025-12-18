using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MToGo.Shared.Security.Cors;
using System.Net;

namespace MToGo.Integration.Tests.Website;

/// <summary>
/// Tests for CORS policy configuration and behavior.
/// These tests verify that CORS headers are correctly set for allowed origins
/// and blocked for non-compliant origins.
/// </summary>
public class CorsPolicyTests
{
    private const string AllowedOrigin = "http://localhost:8081";
    private const string BlockedOrigin = "http://evil-site.com";
    private const string TestEndpoint = "/api/test";

    /// <summary>
    /// Creates a test server with CORS configured using the provided settings.
    /// </summary>
    private static IHost CreateTestServer(CorsSettings corsSettings)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSingleton(corsSettings);
                    services.AddRouting();
                    services.AddCors(options =>
                    {
                        options.AddPolicy(CorsPolicies.TrustedOrigins, builder =>
                        {
                            if (corsSettings.AllowedOrigins.Length > 0)
                            {
                                builder.WithOrigins(corsSettings.AllowedOrigins);
                            }

                            builder.AllowAnyMethod()
                                   .AllowAnyHeader();

                            if (corsSettings.AllowCredentials)
                            {
                                builder.AllowCredentials();
                            }

                            builder.SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.PreflightMaxAgeSeconds));
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseCors(CorsPolicies.TrustedOrigins);
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(TestEndpoint, () => Results.Ok("test"));
                    });
                });
            })
            .Build();
    }

    private static CorsSettings CreateDefaultSettings()
    {
        return new CorsSettings
        {
            AllowedOrigins = new[] { AllowedOrigin },
            AllowCredentials = true,
            PreflightMaxAgeSeconds = 600,
            LogBlockedRequests = true
        };
    }

    private async Task<HttpResponseMessage> SendPreflightRequest(IHost host, string origin, string method = "GET")
    {
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Options, TestEndpoint);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        return await client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendGetRequest(IHost host, string origin)
    {
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("Origin", origin);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task PreflightRequest_WithAllowedOrigin_ReturnsCorrectCorsHeaders()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendPreflightRequest(host, AllowedOrigin);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task PreflightRequest_WithBlockedOrigin_DoesNotReturnCorsHeaders()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendPreflightRequest(host, BlockedOrigin);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task GetRequest_WithAllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendGetRequest(host, AllowedOrigin);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }

    [Fact]
    public async Task GetRequest_WithBlockedOrigin_DoesNotReturnAccessControlAllowOriginHeader()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendGetRequest(host, BlockedOrigin);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task PreflightRequest_WithAllowedOrigin_ReturnsCorrectMaxAge()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        settings.PreflightMaxAgeSeconds = 3600; // 1 hour
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendPreflightRequest(host, AllowedOrigin);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Max-Age"));
        Assert.Equal("3600", response.Headers.GetValues("Access-Control-Max-Age").First());
    }

    [Fact]
    public async Task PreflightRequest_WithAllowedOrigin_AllowsRequestedMethod()
    {
        // Arrange
        var settings = CreateDefaultSettings();
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendPreflightRequest(host, AllowedOrigin, "POST");

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
        var allowedMethods = response.Headers.GetValues("Access-Control-Allow-Methods").First();
        Assert.Contains("POST", allowedMethods);
    }

    [Theory]
    [InlineData("http://localhost:8081", true)]
    [InlineData("http://localhost:5000", false)]
    [InlineData("https://mtogo.example.com", false)]
    [InlineData("http://malicious-site.com", false)]
    public async Task PreflightRequest_OriginValidation_ReturnsExpectedResult(string origin, bool shouldBeAllowed)
    {
        // Arrange
        var settings = CreateDefaultSettings(); // Only allows http://localhost:8081
        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendPreflightRequest(host, origin);

        // Assert
        if (shouldBeAllowed)
        {
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"),
                $"Origin {origin} should be allowed");
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
        }
        else
        {
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"),
                $"Origin {origin} should be blocked");
        }
    }

    [Theory]
    [InlineData("http://localhost:8081")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://mtogo.example.com")]
    public async Task MultipleAllowedOrigins_EachOriginIsAllowed(string testOrigin)
    {
        // Arrange
        var settings = new CorsSettings
        {
            AllowedOrigins = new[]
            {
                "http://localhost:8081",
                "http://localhost:5000",
                "https://mtogo.example.com"
            },
            AllowCredentials = true,
            PreflightMaxAgeSeconds = 600
        };

        using var host = CreateTestServer(settings);
        await host.StartAsync();

        // Act
        var response = await SendPreflightRequest(host, testOrigin);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"),
            $"Origin {testOrigin} should be allowed");
        Assert.Equal(testOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }
}