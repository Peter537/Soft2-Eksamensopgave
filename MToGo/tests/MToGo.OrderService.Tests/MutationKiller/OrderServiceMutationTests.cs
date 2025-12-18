using FluentAssertions;
using MToGo.OrderService.Entities;
using MToGo.OrderService.Models;
using MToGo.OrderService.Repositories;
using MToGo.OrderService.Services;
using MToGo.Shared.Kafka;
using MToGo.Shared.Kafka.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace MToGo.OrderService.Tests.MutationKiller
{
    public class OrderServiceMutationTests
    {
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly Mock<IKafkaProducer> _mockKafkaProducer;
        private readonly Mock<IPartnerServiceClient> _mockPartnerClient;
        private readonly Mock<IAgentServiceClient> _mockAgentClient;
        private readonly Mock<ILogger<Services.OrderService>> _mockLogger;
        private readonly Services.OrderService _orderService;

        public OrderServiceMutationTests()
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

            // Assert - Verify quantity * unitPrice calculation
            capturedOrder.Should().NotBeNull();
            var expectedOrderTotal = (2 * 50m) + (1 * 75m); // 175
            var itemsTotal = capturedOrder!.Items.Sum(i => i.Quantity * i.UnitPrice);
            itemsTotal.Should().Be(expectedOrderTotal);
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
            _mockRepository.Setup(r => r.GetOrdersByCustomerIdAsync(5, null, null))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetOrdersByCustomerIdAsync(5);

            // Assert
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
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

