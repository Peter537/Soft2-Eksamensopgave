using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.WebSocketPartnerService.Handlers;
using MToGo.WebSocketPartnerService.Services;

namespace MToGo.WebSocketPartnerService.Tests.Handlers;

public class PartnerWebSocketHandlerTests
{
    private readonly PartnerWebSocketHandler _sut;
    private readonly PartnerConnectionManager _connectionManager;
    private readonly Mock<ILogger<PartnerWebSocketHandler>> _loggerMock;

    public PartnerWebSocketHandlerTests()
    {
        var connectionManagerLogger = new Mock<ILogger<PartnerConnectionManager>>();
        _connectionManager = new PartnerConnectionManager(connectionManagerLogger.Object);
        _loggerMock = new Mock<ILogger<PartnerWebSocketHandler>>();
        _sut = new PartnerWebSocketHandler(_connectionManager, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleConnectionAsync_ShouldReturn400_WhenNotWebSocketRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var partnerId = 1;

        // Act
        await _sut.HandleConnectionAsync(context, partnerId);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleConnectionAsync_ShouldRegisterAndRemoveConnection_WhenClientCloses()
    {
        // Arrange
        var partnerId = 1;
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
        await _sut.HandleConnectionAsync(contextMock.Object, partnerId);

        // Assert - after handler completes, connection should be removed
        Assert.False(_connectionManager.IsConnected(partnerId));
        Assert.Equal(0, _connectionManager.ConnectionCount);
    }
}
