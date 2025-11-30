// WebSocket client for agent available jobs notifications
window.agentWebSocket = {
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
            case 'OrderAccepted':
                // New job available - partner accepted an order
                const acceptedPayload = message.payload;
                const items = (acceptedPayload.items || []).map(item => ({
                    name: item.name,
                    quantity: item.quantity
                }));
                this.dotNetRef.invokeMethodAsync(
                    'OnOrderAccepted',
                    acceptedPayload.orderId,
                    acceptedPayload.partnerName,
                    acceptedPayload.partnerAddress,
                    acceptedPayload.deliveryAddress,
                    acceptedPayload.deliveryFee,
                    acceptedPayload.distance || '',
                    acceptedPayload.estimatedMinutes || 0,
                    items
                );
                break;

            case 'AgentAssigned':
                // Job was taken by another agent - remove from list
                const assignedPayload = message.payload;
                this.dotNetRef.invokeMethodAsync(
                    'OnAgentAssigned',
                    assignedPayload.orderId,
                    assignedPayload.agentId
                );
                break;

            case 'OrderReady':
                // Food is ready for pickup (for personal room)
                const readyPayload = message.payload;
                this.dotNetRef.invokeMethodAsync(
                    'OnOrderReady',
                    readyPayload.orderId,
                    readyPayload.partnerName,
                    readyPayload.partnerAddress
                );
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

// Separate WebSocket client for Agent's MyDeliveries page (personal room)
window.agentDeliveriesWebSocket = {
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
            case 'DeliveryAccepted':
                // Agent accepted a new delivery - add to their list
                const acceptedPayload = message.payload;
                const items = (acceptedPayload.items || []).map(item => ({
                    name: item.name,
                    quantity: item.quantity
                }));
                this.dotNetRef.invokeMethodAsync(
                    'OnDeliveryAccepted',
                    acceptedPayload.orderId,
                    acceptedPayload.partnerName,
                    acceptedPayload.partnerAddress,
                    acceptedPayload.deliveryAddress,
                    acceptedPayload.deliveryFee,
                    items
                );
                break;

            case 'OrderReady':
                // Food is ready for pickup
                const readyPayload = message.payload;
                this.dotNetRef.invokeMethodAsync(
                    'OnOrderReady',
                    readyPayload.orderId,
                    readyPayload.partnerName,
                    readyPayload.partnerAddress
                );
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
