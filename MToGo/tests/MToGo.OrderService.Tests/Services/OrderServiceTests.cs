using FluentAssertions;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Repositories;
using MToGo.OrderService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace MToGo.OrderService.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly Mock<IKafkaProducer> _mockKafkaProducer;
        private readonly Mock<IPartnerServiceClient> _mockPartnerClient;
        private readonly Mock<IAgentServiceClient> _mockAgentClient;
        private readonly Mock<ILogger<OrderService.Services.OrderService>> _mockLogger;
        private readonly OrderService.Services.OrderService _orderService;

        public OrderServiceTests()
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

        #region CreateOrderAsync Tests

        [Fact]
        public async Task CreateOrderAsync_ShouldReturnOrderId()
        {
            // Arrange
            var request = CreateValidOrderRequest();
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) => { o.Id = 42; return o; });

            // Act
            var result = await _orderService.CreateOrderAsync(request);

            // Assert - Verify the returned ID matches what repository returned
            result.Id.Should().Be(42);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldCalculateTotalAmountCorrectly()
        {
            // Arrange
            var request = new OrderCreateRequest
            {
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test Address",
                DeliveryFee = 29m,
                Distance = "5 km",
                Items = new List<OrderCreateItem>
                {
                    new() { FoodItemId = 1, Name = "Item1", Quantity = 2, UnitPrice = 50m }, // 100
                    new() { FoodItemId = 2, Name = "Item2", Quantity = 1, UnitPrice = 75m }  // 75
                }
            };
            // OrderTotal = 175, ServiceFee = sliding scale, DeliveryFee = 29

            Order? capturedOrder = null;
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .Callback<Order>(o => capturedOrder = o)
                .ReturnsAsync((Order o) => { o.Id = 1; return o; });

            // Act
            await _orderService.CreateOrderAsync(request);

            // Assert - Verify totals (items + service fee + delivery fee)
            capturedOrder.Should().NotBeNull();
            var expectedOrderTotal = (2 * 50m) + (1 * 75m); // 175
            var itemsTotal = capturedOrder!.Items.Sum(i => i.Quantity * i.UnitPrice);
            itemsTotal.Should().Be(expectedOrderTotal);

            var expectedServiceFeeRate = 0.06m - (expectedOrderTotal - 100m) / 900m * 0.03m;
            var expectedServiceFee = expectedOrderTotal * expectedServiceFeeRate;
            capturedOrder.ServiceFee.Should().BeApproximately(expectedServiceFee, 0.000001m);

            var expectedTotalAmount = expectedOrderTotal + expectedServiceFee + request.DeliveryFee;
            capturedOrder.TotalAmount.Should().BeApproximately(expectedTotalAmount, 0.000001m);
        }

        [Fact]
        public async Task CreateOrderAsync_WhenOrderTotalIsHigh_ShouldApplyThreePercentServiceFee()
        {
            // Arrange
            var request = new OrderCreateRequest
            {
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test Address",
                DeliveryFee = 29m,
                Distance = "5 km",
                Items = new List<OrderCreateItem>
                {
                    new() { FoodItemId = 1, Name = "Expensive", Quantity = 3, UnitPrice = 500m } // 1500
                }
            };

            Order? capturedOrder = null;
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .Callback<Order>(o => capturedOrder = o)
                .ReturnsAsync((Order o) => { o.Id = 1; return o; });

            // Act
            await _orderService.CreateOrderAsync(request);

            // Assert
            capturedOrder.Should().NotBeNull();
            var expectedOrderTotal = 1500m;
            capturedOrder!.Items.Sum(i => i.Quantity * i.UnitPrice).Should().Be(expectedOrderTotal);
            capturedOrder.ServiceFee.Should().Be(expectedOrderTotal * 0.03m);
            capturedOrder.TotalAmount.Should().Be(expectedOrderTotal + (expectedOrderTotal * 0.03m) + 29m);
        }

        [Fact]
        public async Task CreateOrderAsync_WhenOrderTotalIsLow_ShouldApplySixPercentServiceFee()
        {
            // Arrange
            var request = new OrderCreateRequest
            {
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test Address",
                DeliveryFee = 10m,
                Distance = "1 km",
                Items = new List<OrderCreateItem>
                {
                    new() { FoodItemId = 1, Name = "Cheap", Quantity = 1, UnitPrice = 50m } // 50
                }
            };

            Order? capturedOrder = null;
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .Callback<Order>(o => capturedOrder = o)
                .ReturnsAsync((Order o) => { o.Id = 1; return o; });

            // Act
            await _orderService.CreateOrderAsync(request);

            // Assert
            capturedOrder.Should().NotBeNull();
            var expectedOrderTotal = 50m;
            capturedOrder!.Items.Sum(i => i.Quantity * i.UnitPrice).Should().Be(expectedOrderTotal);
            capturedOrder.ServiceFee.Should().Be(expectedOrderTotal * 0.06m);
            capturedOrder.TotalAmount.Should().Be(expectedOrderTotal + (expectedOrderTotal * 0.06m) + 10m);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldPublishKafkaEventWithCorrectData()
        {
            // Arrange
            var request = CreateValidOrderRequest();
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) => { o.Id = 123; o.CreatedAt = DateTime.UtcNow; return o; });

            OrderCreatedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderCreated,
                It.IsAny<string>(),
                It.IsAny<OrderCreatedEvent>()))
                .Callback<string, string, OrderCreatedEvent>((t, k, e) => capturedEvent = e);

            // Act
            await _orderService.CreateOrderAsync(request);

            // Assert - Verify event data
            capturedEvent.Should().NotBeNull();
            capturedEvent!.OrderId.Should().Be(123);
            capturedEvent.PartnerId.Should().Be(request.PartnerId);
            capturedEvent.Items.Should().HaveCount(request.Items.Count);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldPublishKafkaEventWithMappedItemsAndRoundtripTimestamp()
        {
            // Arrange
            var request = new OrderCreateRequest
            {
                CustomerId = 1,
                PartnerId = 2,
                DeliveryAddress = "Test Address",
                DeliveryFee = 10m,
                Distance = "5 km",
                Items = new List<OrderCreateItem>
                {
                    new() { FoodItemId = 1, Name = "Item1", Quantity = 2, UnitPrice = 50m },
                    new() { FoodItemId = 2, Name = "Item2", Quantity = 1, UnitPrice = 75m }
                }
            };

            var createdAt = new DateTime(2025, 12, 19, 10, 11, 12, DateTimeKind.Utc);
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync((Order o) =>
                {
                    o.Id = 123;
                    o.CreatedAt = createdAt;
                    return o;
                });

            OrderCreatedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderCreated,
                It.IsAny<string>(),
                It.IsAny<OrderCreatedEvent>()))
                .Callback<string, string, OrderCreatedEvent>((t, k, e) => capturedEvent = e);

            // Act
            await _orderService.CreateOrderAsync(request);

            // Assert
            capturedEvent.Should().NotBeNull();
            capturedEvent!.OrderCreatedTime.Should().Be(createdAt.ToString("O"));
            capturedEvent.OrderCreatedTime.Should().EndWith("Z");
            capturedEvent.Items.Should().HaveCount(2);
            capturedEvent.Items[0].Name.Should().Be("Item1");
            capturedEvent.Items[0].Quantity.Should().Be(2);
            capturedEvent.Items[1].Name.Should().Be("Item2");
            capturedEvent.Items[1].Quantity.Should().Be(1);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldMapItemsCorrectly()
        {
            // Arrange
            var request = new OrderCreateRequest
            {
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test",
                DeliveryFee = 10m,
                Distance = "1 km",
                Items = new List<OrderCreateItem>
                {
                    new() { FoodItemId = 5, Name = "Pizza", Quantity = 3, UnitPrice = 100m }
                }
            };

            Order? capturedOrder = null;
            _mockRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .Callback<Order>(o => capturedOrder = o)
                .ReturnsAsync((Order o) => { o.Id = 1; return o; });

            // Act
            await _orderService.CreateOrderAsync(request);

            // Assert - Verify each item property is mapped
            capturedOrder!.Items.Should().HaveCount(1);
            var item = capturedOrder.Items.First();
            item.FoodItemId.Should().Be(5);
            item.Name.Should().Be("Pizza");
            item.Quantity.Should().Be(3);
            item.UnitPrice.Should().Be(100m);
        }

        #endregion

        #region AcceptOrderAsync Tests

        [Fact]
        public async Task AcceptOrderAsync_WhenOrderNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.AcceptOrderAsync(1, 30);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AcceptOrderAsync_WhenStatusNotPlaced_ShouldReturnFalse()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.AcceptOrderAsync(1, 30);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AcceptOrderAsync_WhenValid_ShouldReturnTrue()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            var result = await _orderService.AcceptOrderAsync(1, 30);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AcceptOrderAsync_ShouldSetStatusToAccepted()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            await _orderService.AcceptOrderAsync(1, 30);

            // Assert
            order.Status.Should().Be(OrderStatus.Accepted);
        }

        [Fact]
        public async Task AcceptOrderAsync_ShouldSetEstimatedMinutes()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            await _orderService.AcceptOrderAsync(1, 45);

            // Assert
            order.EstimatedMinutes.Should().Be(45);
        }

        [Fact]
        public async Task AcceptOrderAsync_ShouldPublishOrderAcceptedEvent()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "TestPartner", Address = "TestAddr" });

            OrderAcceptedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderAccepted,
                It.IsAny<string>(),
                It.IsAny<OrderAcceptedEvent>()))
                .Callback<string, string, OrderAcceptedEvent>((t, k, e) => capturedEvent = e);

            // Act
            await _orderService.AcceptOrderAsync(1, 30);

            // Assert
            capturedEvent.Should().NotBeNull();
            capturedEvent!.PartnerName.Should().Be("TestPartner");
            capturedEvent.PartnerAddress.Should().Be("TestAddr");
            capturedEvent.Timestamp.Should().EndWith("Z");
            capturedEvent.Items.Should().HaveCount(1);
            capturedEvent.Items[0].Name.Should().Be(order.Items[0].Name);
            capturedEvent.Items[0].Quantity.Should().Be(order.Items[0].Quantity);

            _mockRepository.Verify(r => r.UpdateOrderAsync(It.Is<Order>(o => o == order)), Times.Once);
        }

        [Fact]
        public async Task AcceptOrderAsync_WhenPartnerNotFound_ShouldPublishOrderAcceptedEventWithEmptyPartnerFields()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((PartnerResponse?)null);

            OrderAcceptedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderAccepted,
                It.IsAny<string>(),
                It.IsAny<OrderAcceptedEvent>()))
                .Callback<string, string, OrderAcceptedEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.AcceptOrderAsync(1, 30);

            // Assert
            result.Should().BeTrue();
            capturedEvent.Should().NotBeNull();
            capturedEvent!.PartnerName.Should().Be(string.Empty);
            capturedEvent.PartnerAddress.Should().Be(string.Empty);
        }

        #endregion

        #region RejectOrderAsync Tests

        [Fact]
        public async Task RejectOrderAsync_WhenOrderNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.RejectOrderAsync(1, "reason");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RejectOrderAsync_WhenStatusNotPlaced_ShouldReturnFalse()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.RejectOrderAsync(1, "reason");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RejectOrderAsync_WhenValid_ShouldReturnTrue()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.RejectOrderAsync(1, "reason");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RejectOrderAsync_ShouldSetStatusToRejected()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            await _orderService.RejectOrderAsync(1, "reason");

            // Assert
            order.Status.Should().Be(OrderStatus.Rejected);
        }

        [Fact]
        public async Task RejectOrderAsync_WithNullReason_ShouldUseDefaultMessage()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            OrderRejectedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderRejected,
                It.IsAny<string>(),
                It.IsAny<OrderRejectedEvent>()))
                .Callback<string, string, OrderRejectedEvent>((t, k, e) => capturedEvent = e);

            // Act
            await _orderService.RejectOrderAsync(1, null);

            // Assert
            capturedEvent!.Reason.Should().Be("No reason provided");
        }

        [Fact]
        public async Task RejectOrderAsync_ShouldSanitizeNewlines()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            OrderRejectedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderRejected,
                It.IsAny<string>(),
                It.IsAny<OrderRejectedEvent>()))
                .Callback<string, string, OrderRejectedEvent>((t, k, e) => capturedEvent = e);

            // Act
            await _orderService.RejectOrderAsync(1, "Line1\r\nLine2\nLine3");

            // Assert - Newlines should be sanitized
            capturedEvent!.Reason.Should().NotContain("\r");
            capturedEvent.Reason.Should().NotContain("\n");
            capturedEvent.Reason.Should().Be("Line1 Line2 Line3");
            capturedEvent.Timestamp.Should().EndWith("Z");

            _mockRepository.Verify(r => r.UpdateOrderAsync(It.Is<Order>(o => o == order)), Times.Once);
        }

        #endregion

        #region SetReadyAsync Tests

        [Fact]
        public async Task SetReadyAsync_WhenOrderNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.SetReadyAsync(1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SetReadyAsync_WhenStatusNotAccepted_ShouldReturnFalse()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.SetReadyAsync(1);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SetReadyAsync_WhenValid_ShouldReturnTrue()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            var result = await _orderService.SetReadyAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task SetReadyAsync_WhenPartnerFound_ShouldPublishOrderReadyEventWithPartnerFieldsAndTimestamp()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            order.Id = 42;
            order.CustomerId = 5;
            order.PartnerId = 99;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(99))
                .ReturnsAsync(new PartnerResponse { Name = "PartnerName", Address = "PartnerAddr" });

            OrderReadyEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderReady,
                It.IsAny<string>(),
                It.IsAny<OrderReadyEvent>()))
                .Callback<string, string, OrderReadyEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.SetReadyAsync(1);

            // Assert
            result.Should().BeTrue();
            capturedEvent.Should().NotBeNull();
            capturedEvent!.OrderId.Should().Be(42);
            capturedEvent.CustomerId.Should().Be(5);
            capturedEvent.PartnerName.Should().Be("PartnerName");
            capturedEvent.PartnerAddress.Should().Be("PartnerAddr");
            capturedEvent.Timestamp.Should().EndWith("Z");

            _mockRepository.Verify(r => r.UpdateOrderAsync(It.Is<Order>(o => o == order)), Times.Once);
        }

        [Fact]
        public async Task SetReadyAsync_ShouldSetStatusToReady()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            await _orderService.SetReadyAsync(1);

            // Assert
            order.Status.Should().Be(OrderStatus.Ready);
        }

        [Fact]
        public async Task SetReadyAsync_WhenPartnerNotFound_ShouldPublishOrderReadyEventWithEmptyPartnerFields()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((PartnerResponse?)null);

            OrderReadyEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderReady,
                It.IsAny<string>(),
                It.IsAny<OrderReadyEvent>()))
                .Callback<string, string, OrderReadyEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.SetReadyAsync(1);

            // Assert
            result.Should().BeTrue();
            capturedEvent.Should().NotBeNull();
            capturedEvent!.PartnerName.Should().Be(string.Empty);
            capturedEvent.PartnerAddress.Should().Be(string.Empty);
        }

        #endregion

        #region AssignAgentAsync Tests

        [Fact]
        public async Task AssignAgentAsync_WhenOrderNotFound_ShouldReturnOrderNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.AssignAgentAsync(1, 10);

            // Assert
            result.Should().Be(AssignAgentResult.OrderNotFound);
        }

        [Fact]
        public async Task AssignAgentAsync_WhenStatusIsPlaced_ShouldReturnInvalidStatus()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.AssignAgentAsync(1, 10);

            // Assert
            result.Should().Be(AssignAgentResult.InvalidStatus);
        }

        [Fact]
        public async Task AssignAgentAsync_WhenStatusIsAccepted_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            var result = await _orderService.AssignAgentAsync(1, 10);

            // Assert
            result.Should().Be(AssignAgentResult.Success);
        }

        [Fact]
        public async Task AssignAgentAsync_WhenStatusIsReady_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            var result = await _orderService.AssignAgentAsync(1, 10);

            // Assert
            result.Should().Be(AssignAgentResult.Success);
        }

        [Fact]
        public async Task AssignAgentAsync_WhenAgentAlreadyAssigned_ShouldReturnAgentAlreadyAssigned()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            order.AgentId = 5; // Already assigned
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.AssignAgentAsync(1, 10);

            // Assert
            result.Should().Be(AssignAgentResult.AgentAlreadyAssigned);
        }

        [Fact]
        public async Task AssignAgentAsync_ShouldSetAgentId()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Address" });

            // Act
            await _orderService.AssignAgentAsync(1, 99);

            // Assert
            order.AgentId.Should().Be(99);
        }

        [Fact]
        public async Task AssignAgentAsync_WhenPartnerNotFound_ShouldPublishAgentAssignedEventWithEmptyPartnerFields()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((PartnerResponse?)null);

            AgentAssignedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.AgentAssigned,
                It.IsAny<string>(),
                It.IsAny<AgentAssignedEvent>()))
                .Callback<string, string, AgentAssignedEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.AssignAgentAsync(1, 99);

            // Assert
            result.Should().Be(AssignAgentResult.Success);
            capturedEvent.Should().NotBeNull();
            capturedEvent!.PartnerName.Should().Be(string.Empty);
            capturedEvent.PartnerAddress.Should().Be(string.Empty);
            capturedEvent.AgentId.Should().Be(99);
            capturedEvent.OrderId.Should().Be(order.Id);
            capturedEvent.Timestamp.Should().EndWith("Z");
        }

        [Fact]
        public async Task AssignAgentAsync_WhenPartnerFound_ShouldPublishAgentAssignedEventWithItems()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            order.Id = 55;
            order.PartnerId = 77;
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 1, Name = "Burger", Quantity = 2, UnitPrice = 50m }
            };

            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(77))
                .ReturnsAsync(new PartnerResponse { Name = "Partner", Address = "Addr" });

            AgentAssignedEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.AgentAssigned,
                It.IsAny<string>(),
                It.IsAny<AgentAssignedEvent>()))
                .Callback<string, string, AgentAssignedEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.AssignAgentAsync(1, 99);

            // Assert
            result.Should().Be(AssignAgentResult.Success);
            capturedEvent.Should().NotBeNull();
            capturedEvent!.PartnerName.Should().Be("Partner");
            capturedEvent.PartnerAddress.Should().Be("Addr");
            capturedEvent.Timestamp.Should().EndWith("Z");
            capturedEvent.Items.Should().HaveCount(1);
            capturedEvent.Items[0].Name.Should().Be("Burger");
            capturedEvent.Items[0].Quantity.Should().Be(2);

            _mockRepository.Verify(r => r.UpdateOrderAsync(It.Is<Order>(o => o == order)), Times.Once);
        }

        #endregion

        #region PickupOrderAsync Tests

        [Fact]
        public async Task PickupOrderAsync_WhenOrderNotFound_ShouldReturnOrderNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.PickupOrderAsync(1);

            // Assert
            result.Should().Be(PickupResult.OrderNotFound);
        }

        [Fact]
        public async Task PickupOrderAsync_WhenStatusNotReady_ShouldReturnInvalidStatus()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.PickupOrderAsync(1);

            // Assert
            result.Should().Be(PickupResult.InvalidStatus);
        }

        [Fact]
        public async Task PickupOrderAsync_WhenNoAgentAssigned_ShouldReturnNoAgentAssigned()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.AgentId = null;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.PickupOrderAsync(1);

            // Assert
            result.Should().Be(PickupResult.NoAgentAssigned);
        }

        [Fact]
        public async Task PickupOrderAsync_WhenValid_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockAgentClient.Setup(a => a.GetAgentByIdAsync(10))
                .ReturnsAsync(new AgentResponse { Name = "Agent" });

            // Act
            var result = await _orderService.PickupOrderAsync(1);

            // Assert
            result.Should().Be(PickupResult.Success);
        }

        [Fact]
        public async Task PickupOrderAsync_WhenValid_ShouldPublishOrderPickedUpEventWithAgentNameAndUpdateOrder()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.Id = 101;
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockAgentClient.Setup(a => a.GetAgentByIdAsync(10))
                .ReturnsAsync(new AgentResponse { Name = "AgentName" });

            OrderPickedUpEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderPickedUp,
                It.IsAny<string>(),
                It.IsAny<OrderPickedUpEvent>()))
                .Callback<string, string, OrderPickedUpEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.PickupOrderAsync(1);

            // Assert
            result.Should().Be(PickupResult.Success);
            capturedEvent.Should().NotBeNull();
            capturedEvent!.OrderId.Should().Be(101);
            capturedEvent.AgentName.Should().Be("AgentName");
            capturedEvent.Timestamp.Should().EndWith("Z");

            _mockRepository.Verify(r => r.UpdateOrderAsync(It.Is<Order>(o => o == order)), Times.Once);
        }

        [Fact]
        public async Task PickupOrderAsync_ShouldSetStatusToPickedUp()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockAgentClient.Setup(a => a.GetAgentByIdAsync(10))
                .ReturnsAsync(new AgentResponse { Name = "Agent" });

            // Act
            await _orderService.PickupOrderAsync(1);

            // Assert
            order.Status.Should().Be(OrderStatus.PickedUp);
        }

        [Fact]
        public async Task PickupOrderAsync_WhenAgentNotFound_ShouldPublishOrderPickedUpEventWithEmptyAgentName()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);
            _mockAgentClient.Setup(a => a.GetAgentByIdAsync(10))
                .ReturnsAsync((AgentResponse?)null);

            OrderPickedUpEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderPickedUp,
                It.IsAny<string>(),
                It.IsAny<OrderPickedUpEvent>()))
                .Callback<string, string, OrderPickedUpEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.PickupOrderAsync(1);

            // Assert
            result.Should().Be(PickupResult.Success);
            capturedEvent.Should().NotBeNull();
            capturedEvent!.AgentName.Should().Be(string.Empty);
            capturedEvent.OrderId.Should().Be(order.Id);
            capturedEvent.Timestamp.Should().EndWith("Z");
        }

        #endregion

        #region GetActiveOrdersByAgentIdAsync Tests

        [Fact]
        public async Task GetActiveOrdersByAgentIdAsync_WhenPartnerNotFound_ShouldReturnDefaultPartnerFields()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.PickedUp);
            order.Id = 7;
            order.AgentId = 123;
            order.PartnerId = 456;
            order.DeliveryAddress = "Delivery Addr";
            order.CreatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            _mockRepository.Setup(r => r.GetActiveOrdersByAgentIdAsync(123))
                .ReturnsAsync(new List<Order> { order });
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(456))
                .ReturnsAsync((PartnerResponse?)null);

            // Act
            var result = await _orderService.GetActiveOrdersByAgentIdAsync(123);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(7);
            result[0].PartnerId.Should().Be(456);
            result[0].PartnerName.Should().Be("Unknown Partner");
            result[0].PartnerAddress.Should().Be(string.Empty);
            result[0].DeliveryAddress.Should().Be("Delivery Addr");
            result[0].OrderCreatedTime.Should().Be(order.CreatedAt.ToString("O"));
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].FoodItemId.Should().Be(order.Items[0].FoodItemId);
            result[0].Items[0].Name.Should().Be(order.Items[0].Name);
            result[0].Items[0].Quantity.Should().Be(order.Items[0].Quantity);
            result[0].Items[0].UnitPrice.Should().Be(order.Items[0].UnitPrice);
        }

        [Fact]
        public async Task GetActiveOrdersByAgentIdAsync_WhenPartnerFound_ShouldUsePartnerFields()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.PickedUp);
            order.Id = 8;
            order.AgentId = 123;
            order.PartnerId = 456;
            order.CreatedAt = new DateTime(2025, 12, 19, 3, 4, 5, DateTimeKind.Utc);

            _mockRepository.Setup(r => r.GetActiveOrdersByAgentIdAsync(123))
                .ReturnsAsync(new List<Order> { order });
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(456))
                .ReturnsAsync(new PartnerResponse { Name = "Real Partner", Address = "Real Addr" });

            // Act
            var result = await _orderService.GetActiveOrdersByAgentIdAsync(123);

            // Assert
            result.Should().HaveCount(1);
            result[0].PartnerName.Should().Be("Real Partner");
            result[0].PartnerAddress.Should().Be("Real Addr");
        }

        #endregion

        #region GetAvailableOrdersAsync Tests

        [Fact]
        public async Task GetAvailableOrdersAsync_WhenPartnerNotFound_ShouldUseDefaultPartnerFieldsAndMapItems()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.Id = 99;
            order.PartnerId = 555;
            order.DeliveryAddress = "Customer Street";
            order.Distance = "2 km";
            order.EstimatedMinutes = 17;
            order.CreatedAt = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 1, Name = "Burger", Quantity = 2, UnitPrice = 50m }
            };

            _mockRepository.Setup(r => r.GetAvailableOrdersAsync())
                .ReturnsAsync(new List<Order> { order });
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(555))
                .ReturnsAsync((PartnerResponse?)null);

            // Act
            var result = await _orderService.GetAvailableOrdersAsync();

            // Assert
            result.Should().HaveCount(1);
            result[0].OrderId.Should().Be(99);
            result[0].PartnerName.Should().Be("Unknown");
            result[0].PartnerAddress.Should().Be(string.Empty);
            result[0].DeliveryAddress.Should().Be("Customer Street");
            result[0].Distance.Should().Be("2 km");
            result[0].EstimatedMinutes.Should().Be(17);
            result[0].CreatedAt.Should().Be(order.CreatedAt.ToString("O"));
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].Name.Should().Be("Burger");
            result[0].Items[0].Quantity.Should().Be(2);
        }

        [Fact]
        public async Task GetAvailableOrdersAsync_WhenPartnerFound_ShouldUsePartnerFields()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            order.Id = 100;
            order.PartnerId = 555;
            _mockRepository.Setup(r => r.GetAvailableOrdersAsync())
                .ReturnsAsync(new List<Order> { order });
            _mockPartnerClient.Setup(p => p.GetPartnerByIdAsync(555))
                .ReturnsAsync(new PartnerResponse { Name = "Partner X", Address = "Addr X" });

            // Act
            var result = await _orderService.GetAvailableOrdersAsync();

            // Assert
            result.Should().HaveCount(1);
            result[0].PartnerName.Should().Be("Partner X");
            result[0].PartnerAddress.Should().Be("Addr X");
        }

        #endregion

        #region CompleteDeliveryAsync Tests

        [Fact]
        public async Task CompleteDeliveryAsync_WhenOrderNotFound_ShouldReturnOrderNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.CompleteDeliveryAsync(1);

            // Assert
            result.Should().Be(DeliveryResult.OrderNotFound);
        }

        [Fact]
        public async Task CompleteDeliveryAsync_WhenStatusNotPickedUp_ShouldReturnInvalidStatus()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Ready);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CompleteDeliveryAsync(1);

            // Assert
            result.Should().Be(DeliveryResult.InvalidStatus);
        }

        [Fact]
        public async Task CompleteDeliveryAsync_WhenNoAgentAssigned_ShouldReturnNoAgentAssigned()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.PickedUp);
            order.AgentId = null;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CompleteDeliveryAsync(1);

            // Assert
            result.Should().Be(DeliveryResult.NoAgentAssigned);
        }

        [Fact]
        public async Task CompleteDeliveryAsync_WhenValid_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.PickedUp);
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.CompleteDeliveryAsync(1);

            // Assert
            result.Should().Be(DeliveryResult.Success);
        }

        [Fact]
        public async Task CompleteDeliveryAsync_ShouldSetStatusToDelivered()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.PickedUp);
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            await _orderService.CompleteDeliveryAsync(1);

            // Assert
            order.Status.Should().Be(OrderStatus.Delivered);
        }

        [Fact]
        public async Task CompleteDeliveryAsync_WhenValid_ShouldPublishOrderDeliveredEventWithRoundtripTimestamp()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.PickedUp);
            order.Id = 42;
            order.CustomerId = 5;
            order.AgentId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(42)).ReturnsAsync(order);

            OrderDeliveredEvent? capturedEvent = null;
            _mockKafkaProducer.Setup(k => k.PublishAsync(
                KafkaTopics.OrderDelivered,
                It.IsAny<string>(),
                It.IsAny<OrderDeliveredEvent>()))
                .Callback<string, string, OrderDeliveredEvent>((t, k, e) => capturedEvent = e);

            // Act
            var result = await _orderService.CompleteDeliveryAsync(42);

            // Assert
            result.Should().Be(DeliveryResult.Success);
            capturedEvent.Should().NotBeNull();
            capturedEvent!.OrderId.Should().Be(42);
            capturedEvent.CustomerId.Should().Be(5);
            capturedEvent.Timestamp.Should().EndWith("Z");

            _mockRepository.Verify(r => r.UpdateOrderAsync(It.Is<Order>(o => o == order)), Times.Once);
        }

        #endregion

        #region GetOrderDetailAsync Tests

        [Fact]
        public async Task GetOrderDetailAsync_WhenOrderNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetOrderByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 1, "Customer");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be(GetOrderDetailError.NotFound);
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenCustomerHasAccess_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            order.CustomerId = 5;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 5, "Customer");

            // Assert
            result.Success.Should().BeTrue();
            result.Order.Should().NotBeNull();
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenCustomerDoesNotOwnOrder_ShouldReturnForbidden()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            order.CustomerId = 5;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 99, "Customer");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be(GetOrderDetailError.Forbidden);
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenPartnerHasAccess_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            order.PartnerId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 10, "Partner");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenPartnerDoesNotOwnOrder_ShouldReturnForbidden()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            order.PartnerId = 10;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 99, "Partner");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be(GetOrderDetailError.Forbidden);
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenAgentHasAccess_ShouldReturnSuccess()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            order.AgentId = 15;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 15, "Agent");

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenAgentDoesNotOwnOrder_ShouldReturnForbidden()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            order.AgentId = 15;
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 99, "Agent");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be(GetOrderDetailError.Forbidden);
        }

        [Fact]
        public async Task GetOrderDetailAsync_WhenUnknownRole_ShouldReturnForbidden()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Placed);
            _mockRepository.Setup(r => r.GetOrderByIdAsync(1)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(1, 1, "Admin");

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().Be(GetOrderDetailError.Forbidden);
        }

        [Fact]
        public async Task GetOrderDetailAsync_ShouldReturnCorrectOrderData()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Accepted);
            order.Id = 42;
            order.CustomerId = 5;
            order.PartnerId = 10;
            order.AgentId = 15;
            order.DeliveryAddress = "Test Address";
            order.ServiceFee = 5.5m;
            order.DeliveryFee = 29m;
            order.CreatedAt = new DateTime(2025, 12, 19, 7, 6, 5, DateTimeKind.Utc);
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 100m }
            };
            _mockRepository.Setup(r => r.GetOrderByIdAsync(42)).ReturnsAsync(order);

            // Act
            var result = await _orderService.GetOrderDetailAsync(42, 5, "Customer");

            // Assert
            result.Order!.Id.Should().Be(42);
            result.Order.CustomerId.Should().Be(5);
            result.Order.PartnerId.Should().Be(10);
            result.Order.AgentId.Should().Be(15);
            result.Order.DeliveryAddress.Should().Be("Test Address");
            result.Order.ServiceFee.Should().Be(5.5m);
            result.Order.DeliveryFee.Should().Be(29m);
            result.Order.Status.Should().Be("Accepted");
            result.Order.OrderCreatedTime.Should().Be(order.CreatedAt.ToString("O"));
            result.Order.OrderCreatedTime.Should().EndWith("Z");
            result.Order.Items.Should().HaveCount(1);
            result.Order.Items[0].FoodItemId.Should().Be(1);
            result.Order.Items[0].Name.Should().Be("Pizza");
            result.Order.Items[0].Quantity.Should().Be(2);
            result.Order.Items[0].UnitPrice.Should().Be(100m);
        }

        #endregion

        #region GetOrdersByCustomerIdAsync Tests

        [Fact]
        public async Task GetOrdersByCustomerIdAsync_ShouldReturnMappedOrders()
        {
            // Arrange
            var orders = new List<Order>
            {
                CreateOrder(OrderStatus.Delivered),
                CreateOrder(OrderStatus.Placed)
            };
            orders[0].Id = 1;
            orders[1].Id = 2;
            orders[0].CreatedAt = new DateTime(2025, 12, 19, 0, 0, 0, DateTimeKind.Utc);
            orders[1].CreatedAt = new DateTime(2025, 12, 19, 1, 0, 0, DateTimeKind.Utc);
            _mockRepository.Setup(r => r.GetOrdersByCustomerIdAsync(5, null, null))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetOrdersByCustomerIdAsync(5);

            // Assert
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
            result[0].OrderCreatedTime.Should().Be(orders[0].CreatedAt.ToString("O"));
            result[0].OrderCreatedTime.Should().EndWith("Z");
            result[1].OrderCreatedTime.Should().Be(orders[1].CreatedAt.ToString("O"));
            result[1].OrderCreatedTime.Should().EndWith("Z");
        }

        [Fact]
        public async Task GetOrdersByCustomerIdAsync_ShouldMapItemsCorrectly()
        {
            // Arrange
            var order = CreateOrder(OrderStatus.Delivered);
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 100m }
            };
            _mockRepository.Setup(r => r.GetOrdersByCustomerIdAsync(5, null, null))
                .ReturnsAsync(new List<Order> { order });

            // Act
            var result = await _orderService.GetOrdersByCustomerIdAsync(5);

            // Assert
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].FoodItemId.Should().Be(1);
            result[0].Items[0].Name.Should().Be("Pizza");
            result[0].Items[0].Quantity.Should().Be(2);
            result[0].Items[0].UnitPrice.Should().Be(100m);
        }

        #region GetOrdersByAgentIdAsync Tests

        [Fact]
        public async Task GetOrdersByAgentIdAsync_ShouldReturnMappedOrdersAndItems()
        {
            // Arrange
            var createdAt = new DateTime(2025, 12, 19, 9, 8, 7, DateTimeKind.Utc);
            var order = CreateOrder(OrderStatus.Delivered);
            order.Id = 11;
            order.AgentId = 22;
            order.CustomerId = 33;
            order.PartnerId = 44;
            order.DeliveryAddress = "Addr";
            order.CreatedAt = createdAt;
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 9, Name = "Soda", Quantity = 3, UnitPrice = 12m }
            };

            _mockRepository.Setup(r => r.GetOrdersByAgentIdAsync(22, null, null))
                .ReturnsAsync(new List<Order> { order });

            // Act
            var result = await _orderService.GetOrdersByAgentIdAsync(22);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(11);
            result[0].CustomerId.Should().Be(33);
            result[0].PartnerId.Should().Be(44);
            result[0].DeliveryAddress.Should().Be("Addr");
            result[0].OrderCreatedTime.Should().Be(createdAt.ToString("O"));
            result[0].OrderCreatedTime.Should().EndWith("Z");
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].FoodItemId.Should().Be(9);
            result[0].Items[0].Name.Should().Be("Soda");
            result[0].Items[0].Quantity.Should().Be(3);
            result[0].Items[0].UnitPrice.Should().Be(12m);
        }

        #endregion

        #region GetOrdersByPartnerIdAsync Tests

        [Fact]
        public async Task GetOrdersByPartnerIdAsync_ShouldReturnMappedOrdersAndItems()
        {
            // Arrange
            var createdAt = new DateTime(2025, 12, 19, 6, 5, 4, DateTimeKind.Utc);
            var order = CreateOrder(OrderStatus.Delivered);
            order.Id = 101;
            order.PartnerId = 10;
            order.CustomerId = 20;
            order.AgentId = 30;
            order.DeliveryAddress = "Delivery";
            order.CreatedAt = createdAt;
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 1, Name = "Pizza", Quantity = 2, UnitPrice = 100m }
            };

            _mockRepository.Setup(r => r.GetOrdersByPartnerIdAsync(10, null, null))
                .ReturnsAsync(new List<Order> { order });

            // Act
            var result = await _orderService.GetOrdersByPartnerIdAsync(10);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(101);
            result[0].CustomerId.Should().Be(20);
            result[0].AgentId.Should().Be(30);
            result[0].DeliveryAddress.Should().Be("Delivery");
            result[0].OrderCreatedTime.Should().Be(createdAt.ToString("O"));
            result[0].OrderCreatedTime.Should().EndWith("Z");
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].FoodItemId.Should().Be(1);
            result[0].Items[0].Name.Should().Be("Pizza");
            result[0].Items[0].Quantity.Should().Be(2);
            result[0].Items[0].UnitPrice.Should().Be(100m);
        }

        #endregion

        #region GetActiveOrdersByCustomerIdAsync Tests

        [Fact]
        public async Task GetActiveOrdersByCustomerIdAsync_ShouldReturnMappedOrdersAndItems()
        {
            // Arrange
            var createdAt = new DateTime(2025, 12, 19, 1, 2, 3, DateTimeKind.Utc);
            var order = CreateOrder(OrderStatus.Accepted);
            order.Id = 77;
            order.CustomerId = 5;
            order.PartnerId = 6;
            order.AgentId = 7;
            order.DeliveryAddress = "Street";
            order.CreatedAt = createdAt;
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 2, Name = "Wrap", Quantity = 1, UnitPrice = 55m }
            };

            _mockRepository.Setup(r => r.GetActiveOrdersByCustomerIdAsync(5))
                .ReturnsAsync(new List<Order> { order });

            // Act
            var result = await _orderService.GetActiveOrdersByCustomerIdAsync(5);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(77);
            result[0].PartnerId.Should().Be(6);
            result[0].AgentId.Should().Be(7);
            result[0].DeliveryAddress.Should().Be("Street");
            result[0].OrderCreatedTime.Should().Be(createdAt.ToString("O"));
            result[0].OrderCreatedTime.Should().EndWith("Z");
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].FoodItemId.Should().Be(2);
            result[0].Items[0].Name.Should().Be("Wrap");
            result[0].Items[0].Quantity.Should().Be(1);
            result[0].Items[0].UnitPrice.Should().Be(55m);
        }

        #endregion

        #region GetActiveOrdersByPartnerIdAsync Tests

        [Fact]
        public async Task GetActiveOrdersByPartnerIdAsync_ShouldReturnMappedOrdersAndItems()
        {
            // Arrange
            var createdAt = new DateTime(2025, 12, 19, 2, 3, 4, DateTimeKind.Utc);
            var order = CreateOrder(OrderStatus.Accepted);
            order.Id = 88;
            order.PartnerId = 10;
            order.CustomerId = 11;
            order.AgentId = 12;
            order.DeliveryAddress = "PartnerDelivery";
            order.CreatedAt = createdAt;
            order.Items = new List<OrderItem>
            {
                new() { FoodItemId = 3, Name = "Salad", Quantity = 4, UnitPrice = 25m }
            };

            _mockRepository.Setup(r => r.GetActiveOrdersByPartnerIdAsync(10))
                .ReturnsAsync(new List<Order> { order });

            // Act
            var result = await _orderService.GetActiveOrdersByPartnerIdAsync(10);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(88);
            result[0].CustomerId.Should().Be(11);
            result[0].AgentId.Should().Be(12);
            result[0].DeliveryAddress.Should().Be("PartnerDelivery");
            result[0].OrderCreatedTime.Should().Be(createdAt.ToString("O"));
            result[0].OrderCreatedTime.Should().EndWith("Z");
            result[0].Items.Should().HaveCount(1);
            result[0].Items[0].FoodItemId.Should().Be(3);
            result[0].Items[0].Name.Should().Be("Salad");
            result[0].Items[0].Quantity.Should().Be(4);
            result[0].Items[0].UnitPrice.Should().Be(25m);
        }

        #endregion

        #endregion

        #region Helper Methods

        private OrderCreateRequest CreateValidOrderRequest()
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
                    new() { FoodItemId = 1, Name = "Test Item", Quantity = 1, UnitPrice = 100m }
                }
            };
        }

        private Order CreateOrder(OrderStatus status)
        {
            return new Order
            {
                Id = 1,
                CustomerId = 1,
                PartnerId = 1,
                DeliveryAddress = "Test Address",
                DeliveryFee = 29m,
                ServiceFee = 5m,
                TotalAmount = 134m,
                Distance = "5 km",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new() { FoodItemId = 1, Name = "Test", Quantity = 1, UnitPrice = 100m }
                }
            };
        }

        #endregion
    }
}

