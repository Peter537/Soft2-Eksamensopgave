using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MToGo.Testing;

public static class TestAuthenticationExtensions
{
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

    public static HttpClient CreateAuthenticatedClient<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection>? configureServices = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication();
                configureServices?.Invoke(services);
            });
        }).CreateClient();
    }

    public static WebApplicationFactory<TProgram> WithTestAuthentication<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection>? configureServices = null) where TProgram : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddTestAuthentication();
                configureServices?.Invoke(services);
            });
        });
    }
}
