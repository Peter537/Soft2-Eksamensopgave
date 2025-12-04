using FluentAssertions;
using MToGo.OrderService.Models;
using MToGo.OrderService.Services;
using MToGo.OrderService.Repositories;
using MToGo.Shared.Kafka;
using Moq;
using Microsoft.Extensions.Logging;

namespace MToGo.OrderService.Tests
{
    /// <summary>
    /// Test suite demonstrating Equivalence Partitioning and Boundary Value Analysis
    /// for the service fee calculation logic in OrderService.
    /// 
    /// Business Rules:
    /// - orderTotal ≤ 100 DKK: 6% service fee
    /// - orderTotal ≥ 1000 DKK: 3% service fee
    /// - 100 < orderTotal < 1000: Sliding scale from 6% to 3%
    /// </summary>
    public class ServiceFeeCalculationTests
    {
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly Mock<IKafkaProducer> _mockKafkaProducer;
        private readonly Mock<IPartnerServiceClient> _mockPartnerClient;
        private readonly Mock<IAgentServiceClient> _mockAgentClient;
        private readonly Mock<ILogger<Services.OrderService>> _mockLogger;
        private readonly Services.OrderService _orderService;

        public ServiceFeeCalculationTests()
        {
            _mockRepository = new Mock<IOrderRepository>();
            _mockKafkaProducer = new Mock<IKafkaProducer>();
            _mockPartnerClient = new Mock<IPartnerServiceClient>();
            _mockAgentClient = new Mock<IAgentServiceClient>();
            _mockLogger = new Mock<ILogger<Services.OrderService>>();

            _orderService = new Services.OrderService(
                _mockRepository.Object,
                _mockKafkaProducer.Object,
                _mockPartnerClient.Object,
                _mockAgentClient.Object,
                _mockLogger.Object
            );
        }

        #region Equivalence Partitioning Tests

        /// <summary>
        /// Equivalence Partition 1: Order total ≤ 100 DKK (6% fee)
        /// Tests a typical value within the lowest fee bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal50DKK_ShouldApply6PercentServiceFee()
        {
            // Arrange - Value from Partition 1 (≤ 100)
            var orderTotal = 50m;
            var expectedServiceFee = 3.00m; // 50 * 0.06 = 3.00
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        /// <summary>
        /// Equivalence Partition 2: 100 < Order total < 1000 DKK (Sliding scale)
        /// Tests a typical value within the middle bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal500DKK_ShouldApplySlidingScaleServiceFee()
        {
            // Arrange - Value from Partition 2 (100 < x < 1000)
            var orderTotal = 500m;
            // Formula: rate = 0.06 - (orderTotal - 100) / 900 * 0.03
            // rate = 0.06 - (500 - 100) / 900 * 0.03 = 0.06 - 400/900 * 0.03 = 0.06 - 0.0133... = 0.0466...
            var expectedServiceFee = 23.33m; // 500 * 0.04666... ≈ 23.33
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.01m // Allow small rounding difference
            )), Times.Once);
        }

        /// <summary>
        /// Equivalence Partition 3: Order total ≥ 1000 DKK (3% fee)
        /// Tests a typical value within the highest fee bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal1500DKK_ShouldApply3PercentServiceFee()
        {
            // Arrange - Value from Partition 3 (≥ 1000)
            var orderTotal = 1500m;
            var expectedServiceFee = 45.00m; // 1500 * 0.03 = 45.00
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        #endregion

        #region Boundary Value Analysis Tests

        /// <summary>
        /// BVA: Lower boundary of Partition 1 (minimum valid value)
        /// Tests edge case with minimal order total
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal1DKK_ShouldApply6PercentServiceFee()
        {
            // Arrange - Lower boundary of Partition 1
            var orderTotal = 1m;
            var expectedServiceFee = 0.06m; // 1 * 0.06 = 0.06
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        /// <summary>
        /// BVA: Exact boundary at 100 DKK
        /// Tests the upper limit of 6% fee bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal100DKK_ShouldApply6PercentServiceFee()
        {
            // Arrange - Exact boundary value
            var orderTotal = 100m;
            var expectedServiceFee = 6.00m; // 100 * 0.06 = 6.00
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        /// <summary>
        /// BVA: Just above 100 DKK boundary
        /// Tests the lower limit of sliding scale bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal101DKK_ShouldApplySlidingScaleServiceFee()
        {
            // Arrange - Just above boundary
            var orderTotal = 101m;
            // rate = 0.06 - (101 - 100) / 900 * 0.03 = 0.06 - 1/900 * 0.03 ≈ 0.05996666...
            var expectedServiceFee = 6.06m; // 101 * 0.05996666... ≈ 6.06
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.01m
            )), Times.Once);
        }

        /// <summary>
        /// BVA: Just below 1000 DKK boundary
        /// Tests the upper limit of sliding scale bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal999DKK_ShouldApplySlidingScaleServiceFee()
        {
            // Arrange - Just below boundary
            var orderTotal = 999m;
            // rate = 0.06 - (999 - 100) / 900 * 0.03 = 0.06 - 899/900 * 0.03 ≈ 0.030033...
            var expectedServiceFee = 30.00m; // 999 * 0.030033... ≈ 30.00
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.01m
            )), Times.Once);
        }

        /// <summary>
        /// BVA: Exact boundary at 1000 DKK
        /// Tests the lower limit of 3% fee bracket
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal1000DKK_ShouldApply3PercentServiceFee()
        {
            // Arrange - Exact boundary value
            var orderTotal = 1000m;
            var expectedServiceFee = 30.00m; // 1000 * 0.03 = 30.00
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        /// <summary>
        /// BVA: Just above 1000 DKK boundary
        /// Tests confirmation that 3% fee continues above 1000
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal1001DKK_ShouldApply3PercentServiceFee()
        {
            // Arrange - Just above boundary
            var orderTotal = 1001m;
            var expectedServiceFee = 30.03m; // 1001 * 0.03 = 30.03
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        /// <summary>
        /// BVA: Very large order total
        /// Tests upper boundary behavior with high value
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithOrderTotal10000DKK_ShouldApply3PercentServiceFee()
        {
            // Arrange - High boundary value
            var orderTotal = 10000m;
            var expectedServiceFee = 300.00m; // 10000 * 0.03 = 300.00
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                o.ServiceFee == expectedServiceFee
            )), Times.Once);
        }

        #endregion

        #region Helper Methods

        private OrderCreateRequest CreateOrderRequest(decimal itemPrice)
        {
            return new OrderCreateRequest
            {
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test Address 123",
                DeliveryFee = 29m,
                Distance = "5 km",
                Items = new List<OrderCreateItem>
                {
                    new OrderCreateItem
                    {
                        FoodItemId = 1,
                        Name = "Test Item",
                        Quantity = 1,
                        UnitPrice = itemPrice
                    }
                }
            };
        }

        #endregion
    }
}
