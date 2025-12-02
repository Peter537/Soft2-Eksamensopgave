using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using MToGo.WebSocketCustomerService.Handlers;
using MToGo.WebSocketCustomerService.Services;

namespace MToGo.WebSocketCustomerService.Tests.Handlers;

public class CustomerWebSocketHandlerTests
{
    private readonly CustomerWebSocketHandler _sut;
    private readonly CustomerConnectionManager _connectionManager;
    private readonly Mock<ILogger<CustomerWebSocketHandler>> _loggerMock;

    public CustomerWebSocketHandlerTests()
    {
        var connectionManagerLogger = new Mock<ILogger<CustomerConnectionManager>>();
        _connectionManager = new CustomerConnectionManager(connectionManagerLogger.Object);
        _loggerMock = new Mock<ILogger<CustomerWebSocketHandler>>();
        _sut = new CustomerWebSocketHandler(_connectionManager, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleConnectionAsync_ShouldReturn400_WhenNotWebSocketRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var customerId = 1;

        // Act
        await _sut.HandleConnectionAsync(context, customerId);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleConnectionAsync_ShouldRegisterAndRemoveConnection_WhenClientCloses()
    {
        // Arrange
        var customerId = 42;
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
        await _sut.HandleConnectionAsync(contextMock.Object, customerId);

        // Assert - after handler completes, connection should be removed
        Assert.False(_connectionManager.IsConnected(customerId));
        Assert.Equal(0, _connectionManager.ConnectionCount);
    }
}
