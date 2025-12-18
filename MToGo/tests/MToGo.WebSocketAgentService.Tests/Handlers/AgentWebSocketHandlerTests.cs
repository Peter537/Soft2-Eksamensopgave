using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.WebSocketAgentService.Handlers;
using MToGo.WebSocketAgentService.Services;

namespace MToGo.WebSocketAgentService.Tests.Handlers;

public class AgentWebSocketHandlerTests
{
    private readonly AgentWebSocketHandler _target;
    private readonly AgentConnectionManager _connectionManager;
    private readonly Mock<ILogger<AgentWebSocketHandler>> _loggerMock;

    public AgentWebSocketHandlerTests()
    {
        var connectionManagerLogger = new Mock<ILogger<AgentConnectionManager>>();
        _connectionManager = new AgentConnectionManager(connectionManagerLogger.Object);
        _loggerMock = new Mock<ILogger<AgentWebSocketHandler>>();
        _target = new AgentWebSocketHandler(_connectionManager, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleBroadcastConnectionAsync_ShouldReturn400_WhenNotWebSocketRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _target.HandleBroadcastConnectionAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAgentConnectionAsync_ShouldReturn400_WhenNotWebSocketRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var agentId = 1;

        // Act
        await _target.HandleAgentConnectionAsync(context, agentId);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleBroadcastConnectionAsync_ShouldRegisterAndRemoveConnection_WhenClientCloses()
    {
        // Arrange
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        webSocketMock.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        webSocketMock.Setup(ws => ws.CloseOutputAsync(
            It.IsAny<WebSocketCloseStatus>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Callback(() => webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed))
            .Returns(Task.CompletedTask);

        var webSocketManagerMock = new Mock<WebSocketManager>();
        webSocketManagerMock.Setup(m => m.IsWebSocketRequest).Returns(true);
        webSocketManagerMock.Setup(m => m.AcceptWebSocketAsync()).ReturnsAsync(webSocketMock.Object);

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.WebSockets).Returns(webSocketManagerMock.Object);
        contextMock.Setup(c => c.Response).Returns(new DefaultHttpContext().Response);

        // Act
        await _target.HandleBroadcastConnectionAsync(contextMock.Object);

        // Assert - after handler completes, connection should be removed
        Assert.Equal(0, _connectionManager.BroadcastConnectionCount);
    }

    [Fact]
    public async Task HandleAgentConnectionAsync_ShouldRegisterAndRemoveConnection_WhenClientCloses()
    {
        // Arrange
        var agentId = 42;
        var webSocketMock = new Mock<WebSocket>();
        webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Open);
        webSocketMock.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        webSocketMock.Setup(ws => ws.CloseOutputAsync(
            It.IsAny<WebSocketCloseStatus>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Callback(() => webSocketMock.Setup(ws => ws.State).Returns(WebSocketState.Closed))
            .Returns(Task.CompletedTask);

        var webSocketManagerMock = new Mock<WebSocketManager>();
        webSocketManagerMock.Setup(m => m.IsWebSocketRequest).Returns(true);
        webSocketManagerMock.Setup(m => m.AcceptWebSocketAsync()).ReturnsAsync(webSocketMock.Object);

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(c => c.WebSockets).Returns(webSocketManagerMock.Object);
        contextMock.Setup(c => c.Response).Returns(new DefaultHttpContext().Response);

        // Act
        await _target.HandleAgentConnectionAsync(contextMock.Object, agentId);

        // Assert - after handler completes, connection should be removed
        Assert.False(_connectionManager.IsAgentConnected(agentId));
        Assert.Equal(0, _connectionManager.AgentConnectionCount);
    }
}

