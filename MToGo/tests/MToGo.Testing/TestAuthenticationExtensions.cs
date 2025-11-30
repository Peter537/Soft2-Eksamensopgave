using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MToGo.Testing;

public static class TestAuthenticationExtensions
{
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
                // Remove existing authentication schemes and add test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme, options => { });

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
                // Remove existing authentication schemes and add test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme, options => { });

                // Allow additional service configuration
                configureServices?.Invoke(services);
            });
        });
    }
}
