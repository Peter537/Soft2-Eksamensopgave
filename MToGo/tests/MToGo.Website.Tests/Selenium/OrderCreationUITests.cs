using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

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
    ///   2. Run tests: dotnet test --filter "OrderCreationUITests"
    /// 
    /// These tests are skipped in CI/CD as they require the full infrastructure.
    /// For CI, use unit tests and integration tests instead.
    /// </summary>
    public class OrderCreationUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly string _baseUrl = "http://localhost:5000"; 

        public OrderCreationUITests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless"); 
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            
            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            _driver.Manage().Window.Maximize();
        }

        [Fact(Skip = "Requires running application instance")]
        public void CompleteOrderFlow_CustomerCanBrowseMenuAndPlaceOrder()
        {
            // Arrange - Navigate to login page
            _driver.Navigate().GoToUrl($"{_baseUrl}/login");

            // Act - Login as customer
            LoginAsCustomer("customer@test.com", "password123");

            // Assert - Verify login successful
            _wait.Until(d => d.Url.Contains("/customer") || d.Url.Contains("/"));
            
            // Act - Navigate to partners list
            _driver.Navigate().GoToUrl($"{_baseUrl}/customer/partners");
            
            // Assert - Verify partners page loaded
            _wait.Until(d => d.FindElements(By.CssSelector(".partner-card, .card")).Count > 0);
            
            // Act - Click on first partner to view menu
            var partnerCard = _wait.Until(d => d.FindElement(By.CssSelector(".partner-card, .card")));
            partnerCard.Click();
            
            // Assert - Verify menu page loaded
            _wait.Until(d => d.Url.Contains("/menu"));
            var menuItems = _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")));
            menuItems.Should().NotBeEmpty("Menu should have items");
            
            // Act - Add first item to order
            var addButton = _wait.Until(d => d.FindElement(By.CssSelector("button[class*='btn-outline-primary']")));
            addButton.Click();
            
            // Assert - Verify navigation to order creation page
            _wait.Until(d => d.Url.Contains("/order/create"));
            
            // Act - Fill delivery address
            var addressInput = _wait.Until(d => d.FindElement(By.CssSelector("input[type='text']")));
            addressInput.Clear();
            addressInput.SendKeys("123 Test Street, Copenhagen");
            
            // Act - Select payment method (Strategy Pattern implementation)
            var paymentRadioButtons = _wait.Until(d => d.FindElements(By.CssSelector("input[name='paymentMethod']")));
            paymentRadioButtons.Should().NotBeEmpty("Payment methods should be available");
            
            // Select first payment method (e.g., Credit Card)
            var firstPaymentMethod = paymentRadioButtons.First();
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", firstPaymentMethod);
            
            // Assert - Verify order summary is visible
            var orderSummary = _driver.FindElement(By.CssSelector(".card-body"));
            orderSummary.Text.Should().Contain("Order Summary", "Summary section should be visible");
            
            // Act - Click Place Order button
            var placeOrderButton = _wait.Until(d => d.FindElement(
                By.XPath("//button[contains(text(), 'Place Order') or contains(@class, 'btn-success')]")
            ));
            placeOrderButton.Enabled.Should().BeTrue("Place order button should be enabled");
            placeOrderButton.Click();
            
            // Assert - Verify navigation to order confirmation or active orders
            _wait.Until(d => 
            {
                return d.Url.Contains("/customer/orders") || 
                       d.Url.Contains("/customer/active-orders");
            });
        }

        [Fact(Skip = "Requires running application instance")]
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
            var increaseButton = _driver.FindElement(By.XPath("//button[contains(@class, 'btn') and contains(., '+')]"));
            increaseButton.Click();
            
            // Assert - Verify quantity increased
            _wait.Until(d => 
            {
                var updated = d.FindElement(By.CssSelector(".btn-group .btn.disabled, .btn-group span")).Text;
                return int.Parse(updated) == initialQuantity + 1;
            });
            
            // Act - Click decrease quantity button
            var decreaseButton = _driver.FindElement(By.XPath("//button[contains(@class, 'btn') and contains(., '-')]"));
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

        [Fact(Skip = "Requires running application instance")]
        public void OrderCreation_WithoutRequiredFields_ShowsValidation()
        {
            // Arrange - Setup: Login and navigate to order page with an item
            SetupOrderPageWithItem();
            
            // Act - Leave delivery address empty
            var addressInput = _driver.FindElement(By.CssSelector("input[type='text']"));
            addressInput.Clear();
            
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


        [Fact(Skip = "Requires running application instance")]
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

        private void LoginAsCustomer(string email, string password)
        {
            var emailInput = _wait.Until(d => d.FindElement(By.Id("email")));
            emailInput.SendKeys(email);
            
            var passwordInput = _driver.FindElement(By.Id("password"));
            passwordInput.SendKeys(password);
            
            var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
            loginButton.Click();
        }

        private void SetupOrderPageWithItem()
        {
            // Login as customer
            _driver.Navigate().GoToUrl($"{_baseUrl}/login");
            LoginAsCustomer("customer@test.com", "password123");
            
            // Navigate directly to order creation page with item
            _driver.Navigate().GoToUrl($"{_baseUrl}/order/create?partnerId=1&itemId=1");
            
            // Wait for page to load
            _wait.Until(d => d.FindElements(By.CssSelector("table, .card")).Count > 0);
        }

        #endregion

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
