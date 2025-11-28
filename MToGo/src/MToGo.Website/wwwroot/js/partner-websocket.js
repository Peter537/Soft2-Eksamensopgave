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
        console.log('Connecting to WebSocket:', url);

        try {
            this.socket = new WebSocket(url);

            this.socket.onopen = () => {
                console.log('WebSocket connected');
                this.reconnectAttempts = 0;
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnConnected');
                }
            };

            this.socket.onmessage = (event) => {
                console.log('WebSocket message received:', event.data);
                try {
                    const message = JSON.parse(event.data);
                    this._handleMessage(message);
                } catch (e) {
                    console.error('Failed to parse WebSocket message:', e);
                }
            };

            this.socket.onclose = (event) => {
                console.log('WebSocket closed:', event.code, event.reason);
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnDisconnected', event.reason || 'Connection closed');
                }

                // Attempt reconnection
                if (this.reconnectAttempts < this.maxReconnectAttempts) {
                    this.reconnectAttempts++;
                    console.log(`Reconnecting in ${this.reconnectDelay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
                    setTimeout(() => this._connect(url), this.reconnectDelay);
                }
            };

            this.socket.onerror = (error) => {
                console.error('WebSocket error:', error);
            };
        } catch (e) {
            console.error('Failed to create WebSocket:', e);
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
                console.log('Server closed connection:', message.reason);
                break;

            default:
                console.log('Unknown event type:', message.eventType);
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
