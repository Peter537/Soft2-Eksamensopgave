// WS client for partner order notifications
window.partnerWebSocket = {
    socket: null,
    dotNetRef: null,
    reconnectAttempts: 0,
    maxReconnectAttempts: 5,
    reconnectDelay: 3000,

    connect: function (url, dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.reconnectAttempts = 0;
        this._connect(url);
    },

    _connect: function (url) {
        try {
            this.socket = new WebSocket(url);

            this.socket.onopen = () => {
                this.reconnectAttempts = 0;
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnConnected');
                }
            };

            this.socket.onmessage = (event) => {
                try {
                    const message = JSON.parse(event.data);
                    this._handleMessage(message);
                } catch (e) {
                    // Invalid message format
                }
            };

            this.socket.onclose = (event) => {
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnDisconnected', event.reason || 'Connection closed');
                }

                // Attempt reconnection
                if (this.reconnectAttempts < this.maxReconnectAttempts) {
                    this.reconnectAttempts++;
                    setTimeout(() => this._connect(url), this.reconnectDelay);
                }
            };

            this.socket.onerror = (error) => {
                // Error handled by onclose
            };
        } catch (e) {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnDisconnected', 'Failed to connect');
            }
        }
    },

    _handleMessage: function (message) {
        if (!this.dotNetRef) return;

        switch (message.eventType) {
            case 'OrderCreated':
                const orderPayload = message.payload;
                const items = orderPayload.items.map(item => ({
                    name: item.name,
                    quantity: item.quantity
                }));
                this.dotNetRef.invokeMethodAsync(
                    'OnOrderCreated',
                    orderPayload.orderId,
                    orderPayload.orderCreatedTime,
                    items
                );
                break;

            case 'AgentAssigned':
                const agentPayload = message.payload;
                this.dotNetRef.invokeMethodAsync(
                    'OnAgentAssigned',
                    agentPayload.orderId,
                    agentPayload.agentId
                );
                break;

            case 'OrderPickedUp':
                const pickupPayload = message.payload;
                this.dotNetRef.invokeMethodAsync(
                    'OnOrderPickedUp',
                    pickupPayload.orderId
                );
                break;

            case 'connection_closed':
                break;

            default:
                break;
        }
    },

    disconnect: function () {
        if (this.socket) {
            this.socket.close();
            this.socket = null;
        }
        this.dotNetRef = null;
    }
};
