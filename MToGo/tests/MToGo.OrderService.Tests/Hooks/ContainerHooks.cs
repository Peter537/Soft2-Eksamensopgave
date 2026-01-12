using MToGo.OrderService.Tests.Fixtures;
using MToGo.Testing;
using Reqnroll;

namespace MToGo.OrderService.Tests.Hooks
{
    [Binding]
    public class ContainerHooks
    {
        [BeforeScenario(Order = 0)]
        public async Task BeforeScenario(ScenarioContext scenarioContext)
        {
            var factory = new SharedTestWebApplicationFactory();
            await factory.InitializeDatabaseAsync();
            await factory.CleanupDatabaseAsync();

            // Set up default test authentication (can be overridden per scenario)
            TestAuthenticationHandler.SetTestUser("1", "Customer");

            var client = factory.CreateClient();
            scenarioContext["Factory"] = factory;
            scenarioContext["Client"] = client;
            scenarioContext["KafkaMock"] = factory.KafkaMock;
        }

        [AfterScenario]
        public async Task AfterScenario(ScenarioContext scenarioContext)
        {
            // Clear test authentication
            TestAuthenticationHandler.ClearTestUser();

            if (scenarioContext.TryGetValue("Factory", out SharedTestWebApplicationFactory? factory) && factory != null)
            {
                await factory.DisposeAsync();
            }
        }
    }
}

