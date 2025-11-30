using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.WebSocketAgentService.Services;

namespace MToGo.WebSocketAgentService.Tests.Services;

public class AgentConnectionManagerTests
{
    private readonly AgentConnectionManager _sut;
    private readonly Mock<ILogger<AgentConnectionManager>> _loggerMock;

    public AgentConnectionManagerTests()
    {
        _loggerMock = new Mock<ILogger<AgentConnectionManager>>();
        _sut = new AgentConnectionManager(_loggerMock.Object);
    }

    #region Broadcast Connection Tests

    [Fact]
    public async Task RegisterBroadcastConnectionAsync_ShouldAddConnection()
    {
        // Arrange
        var connectionId = Guid.NewGuid().ToString();
        var webSocketMock = CreateOpenWebSocketMock();

        // Act
        await _sut.RegisterBroadcastConnectionAsync(connectionId, webSocketMock.Object);

        // Assert
        Assert.Equal(1, _sut.BroadcastConnectionCount);
    }

    [Fact]
    public async Task RegisterBroadcastConnectionAsync_ShouldSupportMultipleConnections()
    {
        // Arrange
        var ws1 = CreateOpenWebSocketMock();
        var ws2 = CreateOpenWebSocketMock();
        var ws3 = CreateOpenWebSocketMock();

        // Act
        await _sut.RegisterBroadcastConnectionAsync("conn1", ws1.Object);
        await _sut.RegisterBroadcastConnectionAsync("conn2", ws2.Object);
        await _sut.RegisterBroadcastConnectionAsync("conn3", ws3.Object);

        // Assert
        Assert.Equal(3, _sut.BroadcastConnectionCount);
    }

    [Fact]
    public async Task RemoveBroadcastConnection_ShouldRemoveConnection()
    {
        // Arrange
        var connectionId = "test-connection";
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterBroadcastConnectionAsync(connectionId, webSocketMock.Object);

        // Act
        _sut.RemoveBroadcastConnection(connectionId);

        // Assert
        Assert.Equal(0, _sut.BroadcastConnectionCount);
    }

    [Fact]
    public void RemoveBroadcastConnection_ShouldDoNothing_WhenConnectionNotExists()
    {
        // Act
        _sut.RemoveBroadcastConnection("non-existent");

        // Assert
        Assert.Equal(0, _sut.BroadcastConnectionCount);
    }

    #endregion

    #region Agent Connection Tests

    [Fact]
    public async Task RegisterAgentConnectionAsync_ShouldAddConnection()
    {
        // Arrange
        var agentId = 1;
        var webSocketMock = CreateOpenWebSocketMock();

        // Act
        await _sut.RegisterAgentConnectionAsync(agentId, webSocketMock.Object);

        // Assert
        Assert.True(_sut.IsAgentConnected(agentId));
        Assert.Equal(1, _sut.AgentConnectionCount);
    }

    [Fact]
    public async Task RegisterAgentConnectionAsync_ShouldReplaceConnection_WhenAgentAlreadyConnected()
    {
        // Arrange
        var agentId = 1;
        var oldWebSocketMock = CreateOpenWebSocketMock();
        var newWebSocketMock = CreateOpenWebSocketMock();

        await _sut.RegisterAgentConnectionAsync(agentId, oldWebSocketMock.Object);

        // Act
        await _sut.RegisterAgentConnectionAsync(agentId, newWebSocketMock.Object);

        // Assert
        Assert.True(_sut.IsAgentConnected(agentId));
        Assert.Equal(1, _sut.AgentConnectionCount);

        // Old connection should have been closed
        oldWebSocketMock.Verify(ws => ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAgentConnection_ShouldRemoveAgent()
    {
        // Arrange
        var agentId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterAgentConnectionAsync(agentId, webSocketMock.Object);

        // Act
        _sut.RemoveAgentConnection(agentId);

        // Assert
        Assert.False(_sut.IsAgentConnected(agentId));
        Assert.Equal(0, _sut.AgentConnectionCount);
    }

    [Fact]
    public void RemoveAgentConnection_ShouldDoNothing_WhenAgentNotConnected()
    {
        // Act
        _sut.RemoveAgentConnection(999);

        // Assert
        Assert.Equal(0, _sut.AgentConnectionCount);
    }

    [Fact]
    public void IsAgentConnected_ShouldReturnFalse_WhenAgentNotConnected()
    {
        // Act & Assert
        Assert.False(_sut.IsAgentConnected(999));
    }

    #endregion

    #region SendToAgent Tests

    [Fact]
    public async Task SendToAgentAsync_ShouldReturnFalse_WhenAgentNotConnected()
    {
        // Arrange
        var agentId = 999;
        var payload = new { message = "test" };

        // Act
        var result = await _sut.SendToAgentAsync(agentId, "TestEvent", payload);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SendToAgentAsync_ShouldReturnTrue_WhenMessageSent()
    {
        // Arrange
        var agentId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        await _sut.RegisterAgentConnectionAsync(agentId, webSocketMock.Object);
        var payload = new { orderId = 123 };

        // Act
        var result = await _sut.SendToAgentAsync(agentId, "OrderReady", payload);

        // Assert
        Assert.True(result);
        webSocketMock.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToAgentAsync_ShouldRemoveConnection_WhenWebSocketNotOpen()
    {
        // Arrange
        var agentId = 1;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        await _sut.RegisterAgentConnectionAsync(agentId, webSocketMock.Object);

        // Change state to closed
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        // Act
        var result = await _sut.SendToAgentAsync(agentId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsAgentConnected(agentId));
    }

    [Fact]
    public async Task SendToAgentAsync_ShouldRemoveConnection_WhenSendThrows()
    {
        // Arrange
        var agentId = 1;
        var webSocketMock = CreateOpenWebSocketMock();
        webSocketMock.Setup(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            It.IsAny<WebSocketMessageType>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new WebSocketException("Connection lost"));

        await _sut.RegisterAgentConnectionAsync(agentId, webSocketMock.Object);

        // Act
        var result = await _sut.SendToAgentAsync(agentId, "TestEvent", new { });

        // Assert
        Assert.False(result);
        Assert.False(_sut.IsAgentConnected(agentId));
    }

    #endregion

    #region Broadcast Tests

    [Fact]
    public async Task BroadcastToAllAgentsAsync_ShouldSendToAllConnectedAgents()
    {
        // Arrange
        var ws1 = CreateOpenWebSocketMock();
        var ws2 = CreateOpenWebSocketMock();
        var ws3 = CreateOpenWebSocketMock();

        await _sut.RegisterBroadcastConnectionAsync("conn1", ws1.Object);
        await _sut.RegisterBroadcastConnectionAsync("conn2", ws2.Object);
        await _sut.RegisterBroadcastConnectionAsync("conn3", ws3.Object);

        var payload = new { orderId = 123 };

        // Act
        await _sut.BroadcastToAllAgentsAsync("OrderAccepted", payload);

        // Assert
        ws1.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);

        ws2.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);

        ws3.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastToAllAgentsAsync_ShouldRemoveDeadConnections()
    {
        // Arrange
        var liveWs = CreateOpenWebSocketMock();
        var deadWs = new Mock<WebSocket>();
        deadWs.Setup(ws => ws.State).Returns(WebSocketState.Closed);

        await _sut.RegisterBroadcastConnectionAsync("live", liveWs.Object);
        await _sut.RegisterBroadcastConnectionAsync("dead", deadWs.Object);

        // Act
        await _sut.BroadcastToAllAgentsAsync("TestEvent", new { });

        // Assert - dead connection should be removed
        Assert.Equal(1, _sut.BroadcastConnectionCount);

        // Live connection should have received message
        liveWs.Verify(ws => ws.SendAsync(
            It.IsAny<ArraySegment<byte>>(),
            WebSocketMessageType.Text,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastToAllAgentsAsync_ShouldNotFail_WhenNoConnections()
    {
        // Act - should not throw
        await _sut.BroadcastToAllAgentsAsync("TestEvent", new { });

        // Assert
        Assert.Equal(0, _sut.BroadcastConnectionCount);
    }

    #endregion

    #region Helper Methods

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

    #endregion
}
