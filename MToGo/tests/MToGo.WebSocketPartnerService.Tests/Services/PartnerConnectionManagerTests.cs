using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.WebSocketPartnerService.Services;

namespace MToGo.WebSocketPartnerService.Tests.Services;

public class PartnerConnectionManagerTests
{
    private readonly PartnerConnectionManager _sut;
    private readonly Mock<ILogger<PartnerConnectionManager>> _loggerMock;

    public PartnerConnectionManagerTests()
    {
        _loggerMock = new Mock<ILogger<PartnerConnectionManager>>();
        _sut = new PartnerConnectionManager(_loggerMock.Object);
    }

    [Fact]
    public async Task RegisterConnectionAsync_ShouldAddConnection_WhenPartnerNotConnected()
    {
        // Arrange
        var partnerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();

        // Act
        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Assert
        Assert.True(_sut.IsConnected(partnerId));
        Assert.Equal(1, _sut.ConnectionCount);
    }

    [Fact]
    public async Task RegisterConnectionAsync_ShouldReplaceConnection_WhenPartnerAlreadyConnected()
    {
        // Arrange
        var partnerId = 1;
        var oldWebSocketMock = CreateOpenWebSocketMock();
        var newWebSocketMock = CreateOpenWebSocketMock();

        await _sut.RegisterConnectionAsync(partnerId, oldWebSocketMock.Object);

        // Act
        await _sut.RegisterConnectionAsync(partnerId, newWebSocketMock.Object);

        // Assert
        Assert.True(_sut.IsConnected(partnerId));
        Assert.Equal(1, _sut.ConnectionCount);
        
        // Old connection should have been closed
        oldWebSocketMock.Verify(ws => ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveConnection_ShouldRemovePartner_WhenConnected()
    {
        // Arrange
        var partnerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Act
        _sut.RemoveConnection(partnerId);

        // Assert
        Assert.False(_sut.IsConnected(partnerId));
        Assert.Equal(0, _sut.ConnectionCount);
    }

    [Fact]
    public void RemoveConnection_ShouldDoNothing_WhenPartnerNotConnected()
    {
        // Arrange
        var partnerId = 999;

        // Act
        _sut.RemoveConnection(partnerId);

        // Assert
        Assert.Equal(0, _sut.ConnectionCount);
    }

    [Fact]
    public async Task SendToPartnerAsync_ShouldReturnFalse_WhenPartnerNotConnected()
    {
        // Arrange
        var partnerId = 999;
        var payload = new { message = "test" };

        // Act
        var result = await _sut.SendToPartnerAsync(partnerId, "TestEvent", payload);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendToPartnerAsync_ShouldReturnTrue_WhenMessageSent()
    {
        // Arrange
        var partnerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);
        var payload = new { orderId = 123 };

        // Act
        var result = await _sut.SendToPartnerAsync(partnerId, "OrderCreated", payload);

        // Assert
        Assert.True(result);
        webSocketMock.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToPartnerAsync_ShouldRemoveConnection_WhenWebSocketNotOpen()
    {
        // Arrange
        var partnerId = 1;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Change state to closed
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act
        var result = await _sut.SendToPartnerAsync(partnerId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsConnected(partnerId));
    }

    [Fact]
    public async Task SendToPartnerAsync_ShouldRemoveConnection_WhenSendThrows()
    {
        // Arrange
        var partnerId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        webSocketMock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            It.IsAny<WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("Connection lost"));

        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Act
        var result = await _sut.SendToPartnerAsync(partnerId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsConnected(partnerId));
    }

    [Fact]
    public void IsConnected_ShouldReturnFalse_WhenPartnerNeverConnected()
    {
        // Act
        var result = _sut.IsConnected(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsConnected_ShouldReturnFalse_WhenWebSocketClosed()
    {
        // Arrange
        var partnerId = 1;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Close the socket
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act
        var result = _sut.IsConnected(partnerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ConnectionCount_ShouldTrackMultiplePartners()
    {
        // Arrange & Act
        await _sut.RegisterConnectionAsync(1, CreateOpenWebSocketMock().Object);
        await _sut.RegisterConnectionAsync(2, CreateOpenWebSocketMock().Object);
        await _sut.RegisterConnectionAsync(3, CreateOpenWebSocketMock().Object);

        // Assert
        Assert.Equal(3, _sut.ConnectionCount);

        // Remove one
        _sut.RemoveConnection(2);
        Assert.Equal(2, _sut.ConnectionCount);
    }

    [Fact]
    public async Task ConnectionCleanup_ShouldRemoveClosedConnections_WhenSendAttempted()
    {
        // Arrange - simulate partner closing their browser/tab
        var partnerId = 1;
        var webSocketMock = new Mock<WebSocket>();
        
        // Start as open
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        webSocketMock.Setup(ws => ws.CloseAsync(
            It.IsAny<WebSocketCloseStatus>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);
        Assert.True(_sut.IsConnected(partnerId));

        // Simulate the partner closing their connection (browser closed, network lost, etc.)
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act - try to send a message
        var result = await _sut.SendToPartnerAsync(partnerId, "TestEvent", new { data = "test" });

        // Assert - connection should be cleaned up automatically
        Assert.False(result);
        Assert.False(_sut.IsConnected(partnerId));
        Assert.Equal(0, _sut.ConnectionCount);
    }

    [Fact]
    public async Task ConnectionCleanup_ShouldRemoveAbortedConnections()
    {
        // Arrange - simulate abrupt disconnect
        var partnerId = 1;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        webSocketMock.Setup(ws => ws.CloseAsync(
            It.IsAny<WebSocketCloseStatus>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.RegisterConnectionAsync(partnerId, webSocketMock.Object);

        // Connection aborts (network failure, etc.)
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Aborted);

        // Act
        var result = await _sut.SendToPartnerAsync(partnerId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsConnected(partnerId));
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
