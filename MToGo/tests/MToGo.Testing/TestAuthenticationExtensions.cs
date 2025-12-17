using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MToGo.Testing;

public static class TestAuthenticationExtensions
{
    /// <summary>
    /// Adds test authentication to the service collection.
    /// Use this in ConfigureServices when setting up WebApplicationFactory.
    /// Use TestAuthenticationHandler.SetTestUser() before making requests to set up the authenticated user.
    /// </summary>
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
            options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
        })
        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
            TestAuthenticationHandler.AuthenticationScheme, _ => { });

        return services;
    }

    /// <summary>
    /// Creates an HttpClient with test authentication configured.
    /// Use TestAuthenticationHandler.SetTestUser() before making requests to set up the authenticated user.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection>? configureServices = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication();

                // Allow additional service configuration
                configureServices?.Invoke(services);
            });
        }).CreateClient();
    }

    /// <summary>
    /// Creates a WebApplicationFactory with test authentication configured.
    /// </summary>
    public static WebApplicationFactory<TProgram> WithTestAuthentication<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection>? configureServices = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication();

                // Allow additional service configuration
                configureServices?.Invoke(services);
            });
        });
    }
}

