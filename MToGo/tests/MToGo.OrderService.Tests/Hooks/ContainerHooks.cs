using MToGo.OrderService.Tests.Fixtures;
using MToGo.Testing;
using Reqnroll;

namespace MToGo.OrderService.Tests.Hooks
{
    [Binding]
    public class ContainerHooks
    {
        private static SharedContainerFixture? _containerFixture;
        private static readonly SemaphoreSlim _initLock = new(1, 1);
        private static bool _isInitialized;

        [BeforeScenario(Order = 0)]
        public async Task BeforeScenario(ScenarioContext scenarioContext)
        {
            await EnsureContainersStartedAsync();

            var factory = new SharedTestWebApplicationFactory(_containerFixture!);
            await factory.InitializeDatabaseAsync();
            await factory.CleanupDatabaseAsync();

            // Set up default test authentication (can be overridden per scenario)
            TestAuthenticationHandler.SetTestUser("1", "Customer");

            var client = factory.CreateClient();

            scenarioContext["ContainerFixture"] = _containerFixture;
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

        [AfterTestRun]
        public static async Task AfterTestRun()
        {
            if (_containerFixture != null)
            {
                await _containerFixture.DisposeAsync();
                _containerFixture = null;
                _isInitialized = false;
            }
        }

        private static async Task EnsureContainersStartedAsync()
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    _containerFixture = new SharedContainerFixture();
                    await _containerFixture.InitializeAsync();
                    _isInitialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }
    }
}
