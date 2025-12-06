using System.Net.WebSockets;

namespace MToGo.WebSocketPartnerService.Models
{
    public class PartnerConnection
    {
        public WebSocket WebSocket { get; }
        public DateTime ConnectedAt { get; }

        public PartnerConnection(WebSocket webSocket)
        {
            WebSocket = webSocket;
            ConnectedAt = DateTime.UtcNow;
        }
    }
}
