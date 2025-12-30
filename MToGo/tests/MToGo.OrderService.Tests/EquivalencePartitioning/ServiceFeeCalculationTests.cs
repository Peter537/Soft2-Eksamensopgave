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
        private readonly Mock<ILogger<OrderService.Services.OrderService>> _mockLogger;
        private readonly OrderService.Services.OrderService _orderService;

        public ServiceFeeCalculationTests()
        {
            _mockRepository = new Mock<IOrderRepository>();
            _mockKafkaProducer = new Mock<IKafkaProducer>();
            _mockPartnerClient = new Mock<IPartnerServiceClient>();
            _mockAgentClient = new Mock<IAgentServiceClient>();
            _mockLogger = new Mock<ILogger<OrderService.Services.OrderService>>();

            _orderService = new OrderService.Services.OrderService(
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
            var expectedRate = 0.06m - (orderTotal - 100m) / 900m * 0.03m;
            var expectedServiceFee = orderTotal * expectedRate;
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.000001m
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
        /// BVA (Lower Bound): Partition 1 lower boundary at 1 DKK.
        /// Boundary selection: (-1, 0, +1) => 0, 1, 2.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task CreateOrder_LowerBoundaryAt1DKK_ShouldApplyExpectedServiceFee(decimal orderTotal)
        {
            // Arrange
            var expectedServiceFee = ExpectedServiceFee(orderTotal);
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.000001m
            )), Times.Once);
        }

        /// <summary>
        /// BVA (Upper Bound): Partition 1 upper boundary at 100 DKK.
        /// Boundary selection: (0, +1) => 100, 101.
        /// </summary>
        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        public async Task CreateOrder_UpperBoundaryAt100DKK_ShouldApplyExpectedServiceFee(decimal orderTotal)
        {
            // Arrange
            var expectedServiceFee = ExpectedServiceFee(orderTotal);
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.000001m
            )), Times.Once);
        }

        /// <summary>
        /// BVA (Lower Bound): Partition 2 lower boundary at 101 DKK.
        /// Boundary selection: (-1, 0, +1) => 100, 101, 102.
        /// </summary>
        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        [InlineData(102)]
        public async Task CreateOrder_LowerBoundaryAt101DKK_ShouldApplyExpectedServiceFee(decimal orderTotal)
        {
            // Arrange
            var expectedServiceFee = ExpectedServiceFee(orderTotal);
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.000001m
            )), Times.Once);
        }

        /// <summary>
        /// BVA (Upper Bound): Partition 2 upper boundary at 1000 DKK.
        /// Boundary selection: (0, +1) => 1000, 1001.
        /// </summary>
        [Theory]
        [InlineData(1000)]
        [InlineData(1001)]
        public async Task CreateOrder_UpperBoundaryAt1000DKK_ShouldApplyExpectedServiceFee(decimal orderTotal)
        {
            // Arrange
            var expectedServiceFee = ExpectedServiceFee(orderTotal);
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.000001m
            )), Times.Once);
        }

        /// <summary>
        /// BVA (Lower Bound): Partition 3 lower boundary at 1001 DKK.
        /// Boundary selection: (-1, 0, +1) => 1000, 1001, 1002.
        /// </summary>
        [Theory]
        [InlineData(1000)]
        [InlineData(1001)]
        [InlineData(1002)]
        public async Task CreateOrder_LowerBoundaryAt1001DKK_ShouldApplyExpectedServiceFee(decimal orderTotal)
        {
            // Arrange
            var expectedServiceFee = ExpectedServiceFee(orderTotal);
            var request = CreateOrderRequest(orderTotal);

            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Entities.Order>()))
                .ReturnsAsync((Entities.Order o) => { o.Id = 1; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockRepository.Verify(r => r.CreateOrderAsync(It.Is<Entities.Order>(o =>
                Math.Abs(o.ServiceFee - expectedServiceFee) < 0.000001m
            )), Times.Once);
        }

        #endregion

        #region Helper Methods

        private static decimal ExpectedServiceFee(decimal orderTotal)
        {
            if (orderTotal <= 100m)
                return orderTotal * 0.06m;
            if (orderTotal >= 1000m)
                return orderTotal * 0.03m;

            decimal rate = 0.06m - (orderTotal - 100m) / 900m * 0.03m;
            return orderTotal * rate;
        }

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

