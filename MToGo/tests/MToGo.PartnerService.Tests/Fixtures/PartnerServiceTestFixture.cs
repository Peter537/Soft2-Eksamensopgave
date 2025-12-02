using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MToGo.PartnerService.Data;
using MToGo.Testing;
using Testcontainers.PostgreSql;

namespace MToGo.PartnerService.Tests.Fixtures;

public class PartnerServiceTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private bool _databaseInitialized;

    public string PostgresConnectionString => _postgresContainer.GetConnectionString();

    public PartnerServiceTestFixture()
    {
        _postgresContainer = PostgreSqlContainerHelper.CreatePostgreSqlContainer();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    public bool IsDatabaseInitialized => _databaseInitialized;
    public void MarkDatabaseInitialized() => _databaseInitialized = true;
}

public class PartnerServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly PartnerServiceTestFixture _fixture;

    public PartnerServiceWebApplicationFactory(PartnerServiceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeDatabaseAsync()
    {
        if (!_fixture.IsDatabaseInitialized)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
            _fixture.MarkDatabaseInitialized();
        }
    }

    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PartnerDbContext>();

        // Clear all data but keep the schema
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"MenuItems\" CASCADE;");
        await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Partners\" CASCADE;");

        // Reset identity sequences for predictable IDs in tests
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"Partners_Id_seq\" RESTART WITH 1;");
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE \"MenuItems_Id_seq\" RESTART WITH 1;");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _fixture.PostgresConnectionString
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Add test authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.AuthenticationScheme, options => { });
        });
    }
}

[CollectionDefinition("PartnerService Integration Tests")]
public class PartnerServiceTestCollection : ICollectionFixture<PartnerServiceTestFixture>
{
}
