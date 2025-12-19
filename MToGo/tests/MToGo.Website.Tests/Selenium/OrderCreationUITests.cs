using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using FluentAssertions;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;

namespace MToGo.Website.Tests.Selenium
{
    /// <summary>
    /// Selenium UI Tests for Order Creation Flow.
    /// 
    /// IMPORTANT: These tests require:
    /// 1. A running MToGo application instance (docker-compose up)
    /// 2. Chrome browser installed
    /// 
    /// To run locally:
    ///   1. Start the application: docker-compose up -d
    ///   2. Run tests: dotnet test --filter "OrderCreationUITests" -e RUN_UI_TESTS=true
    /// 
    /// These tests are skipped in CI/CD as they require the full infrastructure.
    /// For CI, use unit tests and integration tests instead.
    /// </summary>
    public class OrderCreationUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly string _baseUrl = "http://localhost:8081"; 
        private readonly HttpClient _apiClient;
        private readonly string _testCustomerEmail;
        private readonly string _testCustomerPassword = "Password123!";
        private readonly int _partnerId;
        private readonly int _menuItemId;
        private string? _jwtToken;

        // Check if UI tests should run (set RUN_UI_TESTS=true)
        private static bool ShouldRunUITests => 
            Environment.GetEnvironmentVariable("RUN_UI_TESTS")?.ToLower() == "true"; 

        public OrderCreationUITests()
        {
            // Skip test setup if UI tests are not enabled
            if (!ShouldRunUITests)
            {
                throw new Xunit.SkipException("Set RUN_UI_TESTS=true to run UI tests");
            }

            var options = new ChromeOptions();
            options.AddArgument("--headless"); 
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            
            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            _driver.Manage().Window.Maximize();

            // API client talks directly to the API Gateway
            _apiClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

            // Create test data for UI flows
            _testCustomerEmail = $"ui_test_{Guid.NewGuid():N}@example.com";
            RegisterTestCustomer().GetAwaiter().GetResult();
            RegisterTestPartnerWithMenu().GetAwaiter().GetResult();

            // Resolve a partner and menu item that can be used in UI flows
            var (partnerId, menuItemId) = InitializePartnerAndMenuItem().GetAwaiter().GetResult();
            _partnerId = partnerId;
            _menuItemId = menuItemId;
        }

        [SkippableFact]
        public void CompleteOrderFlow_CustomerCanBrowseMenuAndPlaceOrder()
        {
            EnsureAuthenticatedCustomer();
            
            // Act - Navigate directly to a known partner menu (from seeded data)
            _driver.Navigate().GoToUrl($"{_baseUrl}/customer/partners/{_partnerId}/menu");

            // Assert - Verify menu page loaded and has items
            _wait.Until(d => d.Url.Contains($"/customer/partners/{_partnerId}/menu"));
            _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
            var menuItems = _driver.FindElements(By.CssSelector("table tbody tr"));
            menuItems.Should().NotBeEmpty("Menu should have items");

            // Act - Navigate to order creation page for the first menu item
            _driver.Navigate().GoToUrl($"{_baseUrl}/order/create?partnerId={_partnerId}&itemId={_menuItemId}");

            // Assert - Verify navigation to order creation page with items
            _wait.Until(d => d.Url.Contains("/order/create"));
            _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
            
            // Act - Fill delivery address
            var addressInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
            addressInput.Clear();
            addressInput.SendKeys("123 Test Street, Copenhagen");
            // Blazor @bind on <input> defaults to onchange; force a blur so the bound value updates.
            addressInput.SendKeys(Keys.Tab);
            
            // Act - Select payment method (Strategy Pattern implementation)
            _wait.Until(d => d.FindElements(By.CssSelector("input[name='paymentMethod']")).Count > 0);
            var paymentRadioButtons = _driver.FindElements(By.CssSelector("input[name='paymentMethod']"));
            paymentRadioButtons.Should().NotBeEmpty("Payment methods should be available");
            
            // Select first payment method (e.g., Credit Card)
            var firstPaymentMethod = paymentRadioButtons.First();
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", firstPaymentMethod);
            
            // Assert - Verify order summary is visible
            var orderSummary = _driver.FindElement(By.CssSelector(".card-body"));
            orderSummary.Text.Should().Contain("Order Summary", "Summary section should be visible");
            
            // Act - Click Place Order button
            _wait.Until(d =>
            {
                var btn = d.FindElement(By.XPath("//button[contains(text(), 'Place Order') or contains(@class, 'btn-success')]"));
                return btn.Enabled;
            });

            var placeOrderButton = _driver.FindElement(
                By.XPath("//button[contains(text(), 'Place Order') or contains(@class, 'btn-success')]")
            );
            placeOrderButton.Enabled.Should().BeTrue("Place order button should be enabled");
            placeOrderButton.Click();
            
            // Assert - Verify navigation to order confirmation or active orders
            _wait.Until(d => 
            {
                return d.Url.Contains("/customer/orders") || 
                       d.Url.Contains("/customer/active-orders");
            });
        }

        [SkippableFact]
        public void OrderCreation_QuantityControls_WorkCorrectly()
        {
            // Arrange - Setup: Login and navigate to order page with an item
            SetupOrderPageWithItem();
            
            // Act - Find quantity display
            var quantityDisplay = _wait.Until(d => 
                d.FindElement(By.CssSelector(".btn-group .btn.disabled, .btn-group span"))
            );
            var initialQuantity = int.Parse(quantityDisplay.Text);
            
            // Act - Click increase quantity button
            var firstRow = _driver.FindElement(By.CssSelector("table tbody tr"));
            var increaseButton = firstRow.FindElement(By.XPath(".//button[.//i[contains(@class,'bi-plus')]]"));
            increaseButton.Click();
            
            // Assert - Verify quantity increased
            _wait.Until(d => 
            {
                var updated = d.FindElement(By.CssSelector(".btn-group .btn.disabled, .btn-group span")).Text;
                return int.Parse(updated) == initialQuantity + 1;
            });
            
            // Act - Click decrease quantity button
            var decreaseButton = firstRow.FindElement(By.XPath(".//button[.//i[contains(@class,'bi-dash')]]"));
            decreaseButton.Click();
            
            // Assert - Verify quantity decreased back to initial
            _wait.Until(d => 
            {
                var updated = d.FindElement(By.CssSelector(".btn-group .btn.disabled, .btn-group span")).Text;
                return int.Parse(updated) == initialQuantity;
            });
            
            // Act - Try to remove item
            var removeButton = _driver.FindElement(By.CssSelector("button.btn-outline-danger, button[class*='trash']"));
            removeButton.Click();
            
            // Assert - Verify item was removed (order items table should be empty or show warning)
            var orderItems = _driver.FindElements(By.CssSelector("table tbody tr"));
            orderItems.Should().BeEmpty("Item should be removed from order");
        }

        [SkippableFact]
        public void OrderCreation_WithoutRequiredFields_ShowsValidation()
        {
            // Arrange - Setup: Login and navigate to order page with an item
            SetupOrderPageWithItem();
            
            // Act - Leave delivery address empty
            var addressInput = _driver.FindElement(By.CssSelector("input[type='text']"));
            addressInput.Clear();
            addressInput.SendKeys(Keys.Tab);
            
            // Act - Try to place order without payment method
            var placeOrderButton = _driver.FindElement(
                By.XPath("//button[contains(text(), 'Place Order') or contains(@class, 'btn-success')]")
            );
            
            // Assert - Button should be disabled
            placeOrderButton.Enabled.Should().BeFalse(
                "Place order button should be disabled without required fields"
            );
            
            // Assert - Validation message should be visible
            var validationAlert = _wait.Until(d => 
                d.FindElement(By.CssSelector(".alert-info, .alert-warning"))
            );
            validationAlert.Displayed.Should().BeTrue("Validation message should be shown");
        }


        [SkippableFact]
        public void OrderCreation_WithoutAuthentication_RedirectsToLogin()
        {
            // Act - Try to access order creation page without login
            _driver.Navigate().GoToUrl($"{_baseUrl}/order/create?partnerId=1&itemId=1");
            
            // Assert - Should be redirected to login or show authentication warning
            _wait.Until(d => 
                d.Url.Contains("/login") || 
                d.FindElements(By.CssSelector(".alert-warning")).Count > 0
            );
            
            var currentUrl = _driver.Url;
            var hasWarning = _driver.FindElements(By.CssSelector(".alert-warning")).Count > 0;
            
            (currentUrl.Contains("/login") || hasWarning).Should().BeTrue(
                "Unauthenticated users should be redirected to login or see warning"
            );
        }

        #region Helper Methods

        private async Task RegisterTestCustomer()
        {
            var request = new
            {
                Name = "UI Test Customer",
                Email = _testCustomerEmail,
                Password = _testCustomerPassword,
                DeliveryAddress = "Test Street 1",
                PhoneNumber = "12345678",
                NotificationMethod = "Email",
                LanguagePreference = "en"
            };

            var response = await _apiClient.PostAsJsonAsync("/api/v1/customers", request);
            response.EnsureSuccessStatusCode();
        }

        private async Task RegisterTestPartnerWithMenu()
        {
            var partnerRequest = new
            {
                Name = "UI Test Partner",
                Address = "Test Partner Street 1",
                Email = $"ui_test_partner_{Guid.NewGuid():N}@example.com",
                Password = "Password123!",
                Menu = new[]
                {
                    new { Name = "Test Pizza", Price = 89.00m }
                }
            };

            var response = await _apiClient.PostAsJsonAsync("/api/v1/partners", partnerRequest);
            response.EnsureSuccessStatusCode();
        }

        private async Task<(int partnerId, int menuItemId)> InitializePartnerAndMenuItem()
        {
            var partners = await _apiClient.GetFromJsonAsync<List<PublicPartnerResponse>>("/api/v1/partners")
                           ?? new List<PublicPartnerResponse>();

            foreach (var partner in partners)
            {
                var menu = await _apiClient.GetFromJsonAsync<PublicMenuResponse>($"/api/v1/partners/{partner.Id}/menu");

                if (menu?.Items is { Count: > 0 })
                {
                    var firstItem = menu.Items.First();
                    return (partner.Id, firstItem.Id);
                }
            }

            throw new InvalidOperationException("No partner with a non-empty menu was found for UI tests.");
        }

        private void EnsureAuthenticatedCustomer()
        {
            if (string.IsNullOrWhiteSpace(_jwtToken))
            {
                _jwtToken = LoginCustomerViaApi(_testCustomerEmail, _testCustomerPassword)
                    .GetAwaiter()
                    .GetResult();
            }

            // Load a page to ensure we have an active JS context, then seed localStorage before
            // navigating to auth-dependent pages. AuthService reads the token on first render.
            _driver.Navigate().GoToUrl($"{_baseUrl}/");
            WaitForDocumentReady();
            WaitForAuthStorageReady();

            ((IJavaScriptExecutor)_driver).ExecuteScript("window.authStorage.setToken(arguments[0]);", _jwtToken);

            _driver.Navigate().GoToUrl($"{_baseUrl}/profile");
            _wait.Until(d => d.Url.Contains("/profile"));
            _wait.Until(d => d.FindElements(By.CssSelector("input#email")).Count > 0);
        }

        private async Task<string> LoginCustomerViaApi(string email, string password)
        {
            var request = new { Email = email, Password = password };

            var response = await _apiClient.PostAsJsonAsync("/api/v1/customers/login", request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<CustomerLoginResponse>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.Jwt))
            {
                throw new InvalidOperationException("Customer login did not return a JWT.");
            }

            return payload.Jwt;
        }

        private void WaitForDocumentReady()
        {
            _wait.Until(driver =>
            {
                try
                {
                    var js = (IJavaScriptExecutor)driver;
                    return string.Equals(
                        js.ExecuteScript("return document.readyState")?.ToString(),
                        "complete",
                        StringComparison.OrdinalIgnoreCase
                    );
                }
                catch
                {
                    return false;
                }
            });
        }

        private void WaitForAuthStorageReady()
        {
            _wait.Until(driver =>
            {
                try
                {
                    var js = (IJavaScriptExecutor)driver;
                    var result = js.ExecuteScript("return !!window.authStorage && typeof window.authStorage.setToken === 'function';");
                    return result is bool b && b;
                }
                catch
                {
                    return false;
                }
            });
        }

        private void LoginAsCustomer(string email, string password)
        {
            // Ensure Blazor has finished initializing on the login page
            WaitForBlazorReady();

            // Robustly set the login fields, handling potential re-renders
            SetInputValue(By.CssSelector("input#identifier"), email);
            SetInputValue(By.CssSelector("input#password"), password);

            var loginButton = _wait.Until(d =>
            {
                try
                {
                    var element = d.FindElement(By.CssSelector("button[type='submit']"));
                    return (element.Displayed && element.Enabled) ? element : null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            });

            loginButton.Click();
        }

        private void WaitForBlazorReady()
        {
            _wait.Until(driver =>
            {
                try
                {
                    var js = (IJavaScriptExecutor)driver;
                    var result = js.ExecuteScript("return !!window.Blazor;");
                    return result is bool b && b;
                }
                catch
                {
                    return false;
                }
            });
        }

        private void SetInputValue(By locator, string value)
        {
            _wait.Until(driver =>
            {
                try
                {
                    var element = driver.FindElement(locator);
                    if (!element.Displayed || !element.Enabled)
                    {
                        return false;
                    }

                    element.Clear();
                    element.SendKeys(value);

                    var currentValue = element.GetAttribute("value") ?? string.Empty;
                    return string.Equals(currentValue, value, StringComparison.Ordinal);
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
        }

        private void SetupOrderPageWithItem()
        {
            EnsureAuthenticatedCustomer();

            // Navigate directly to order creation page with a known partner and item
            _driver.Navigate().GoToUrl($"{_baseUrl}/order/create?partnerId={_partnerId}&itemId={_menuItemId}");

            // Wait for order creation page to load with items
            _wait.Until(d => d.Url.Contains("/order/create"));
            _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
        }

        #endregion

        #region API Response Models

        private class PublicPartnerResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }

        private class PublicMenuResponse
        {
            public int PartnerId { get; set; }
            public string PartnerName { get; set; } = string.Empty;
            public string PartnerAddress { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public List<PublicMenuItemResponse> Items { get; set; } = new();
        }

        private class PublicMenuItemResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        private class CustomerLoginResponse
        {
            public string Jwt { get; set; } = string.Empty;
        }

        #endregion

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}

