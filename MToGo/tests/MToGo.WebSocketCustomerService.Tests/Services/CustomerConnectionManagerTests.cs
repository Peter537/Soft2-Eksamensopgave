using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.Tests.Services;

public class CustomerConnectionManagerTests
{
    private readonly CustomerConnectionManager _sut;
    private readonly Mock<ILogger<CustomerConnectionManager>> _loggerMock;

    public CustomerConnectionManagerTests()
    {
        _loggerMock = new Mock<ILogger<CustomerConnectionManager>>();
        _sut = new CustomerConnectionManager(_loggerMock.Object);
    }

    [Fact]
    public async Task RegisterConnectionAsync_ShouldAddConnection_WhenCustomerNotConnected()
    {
        // Arrange
        var customerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();

        // Act
        await _sut.RegisterConnectionAsync(customerId, webSocketMock.Object);

        // Assert
        Assert.True(_sut.IsConnected(customerId));
        Assert.Equal(1, _sut.ConnectionCount);
    }

    [Fact]
    public async Task RegisterConnectionAsync_ShouldReplaceConnection_WhenCustomerAlreadyConnected()
    {
        // Arrange
        var customerId = 1;
        var oldWebSocketMock = CreateOpenWebSocketMock();
        var newWebSocketMock = CreateOpenWebSocketMock();

        await _sut.RegisterConnectionAsync(customerId, oldWebSocketMock.Object);

        // Act
        await _sut.RegisterConnectionAsync(customerId, newWebSocketMock.Object);

        // Assert
        Assert.True(_sut.IsConnected(customerId));
        Assert.Equal(1, _sut.ConnectionCount);
        
        // Old connection should have been closed
        oldWebSocketMock.Verify(ws => ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveConnection_ShouldRemoveCustomer_WhenConnected()
    {
        // Arrange
        var customerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterConnectionAsync(customerId, webSocketMock.Object);

        // Act
        _sut.RemoveConnection(customerId);

        // Assert
        Assert.False(_sut.IsConnected(customerId));
        Assert.Equal(0, _sut.ConnectionCount);
    }

    [Fact]
    public void RemoveConnection_ShouldDoNothing_WhenCustomerNotConnected()
    {
        // Arrange
        var customerId = 999;

        // Act
        _sut.RemoveConnection(customerId);

        // Assert
        Assert.Equal(0, _sut.ConnectionCount);
    }

    [Fact]
    public async Task SendToCustomerAsync_ShouldReturnFalse_WhenCustomerNotConnected()
    {
        // Arrange
        var customerId = 999;
        var payload = new { message = "test" };

        // Act
        var result = await _sut.SendToCustomerAsync(customerId, "TestEvent", payload);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendToCustomerAsync_ShouldReturnTrue_WhenMessageSent()
    {
        // Arrange
        var customerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterConnectionAsync(customerId, webSocketMock.Object);
        var payload = new { orderId = 123 };

        // Act
        var result = await _sut.SendToCustomerAsync(customerId, "OrderAccepted", payload);

        // Assert
        Assert.True(result);
        webSocketMock.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToCustomerAsync_ShouldRemoveConnection_WhenWebSocketNotOpen()
    {
        // Arrange
        var customerId = 1;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        await _sut.RegisterConnectionAsync(customerId, webSocketMock.Object);

        // Change state to closed
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act
        var result = await _sut.SendToCustomerAsync(customerId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsConnected(customerId));
    }

    [Fact]
    public async Task SendToCustomerAsync_ShouldRemoveConnection_WhenSendThrows()
    {
        // Arrange
        var customerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        webSocketMock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            It.IsAny<WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("Connection lost"));

        await _sut.RegisterConnectionAsync(customerId, webSocketMock.Object);

        // Act
        var result = await _sut.SendToCustomerAsync(customerId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsConnected(customerId));
    }

    [Fact]
    public void IsConnected_ShouldReturnFalse_WhenCustomerNotConnected()
    {
        // Act & Assert
        Assert.False(_sut.IsConnected(999));
    }

    [Fact]
    public async Task IsConnected_ShouldReturnFalse_WhenWebSocketClosed()
    {
        // Arrange
        var customerId = 1;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        await _sut.RegisterConnectionAsync(customerId, webSocketMock.Object);

        // Change state to closed
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act & Assert
        Assert.False(_sut.IsConnected(customerId));
    }

    [Fact]
    public async Task MultipleCustomers_ShouldBeTrackedIndependently()
    {
        // Arrange
        var ws1 = CreateOpenWebSocketMock();
        var ws2 = CreateOpenWebSocketMock();
        var ws3 = CreateOpenWebSocketMock();

        // Act
        await _sut.RegisterConnectionAsync(1, ws1.Object);
        await _sut.RegisterConnectionAsync(2, ws2.Object);
        await _sut.RegisterConnectionAsync(3, ws3.Object);

        // Assert
        Assert.Equal(3, _sut.ConnectionCount);
        Assert.True(_sut.IsConnected(1));
        Assert.True(_sut.IsConnected(2));
        Assert.True(_sut.IsConnected(3));

        // Remove one
        _sut.RemoveConnection(2);
        Assert.Equal(2, _sut.ConnectionCount);
        Assert.True(_sut.IsConnected(1));
        Assert.False(_sut.IsConnected(2));
        Assert.True(_sut.IsConnected(3));
    }

    private static Mock<WebSocket> CreateOpenWebSocketMock()
    {
        var mock = new Mock<WebSocket>();
        mock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        mock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            It.IsAny<WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(ws => ws.CloseAsync(
            It.IsAny<WebSocketCloseStatus>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }
}
