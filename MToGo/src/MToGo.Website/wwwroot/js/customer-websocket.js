// WebSocket client for customer order notifications
// Supports multiple Blazor component listeners (ActiveOrders, OrderDetail, etc.)
window.customerWebSocket = {
    socket: null,
    listeners: new Map(), // listenerId -> dotNetRef
    reconnectAttempts: 0,
    maxReconnectAttempts: 5,
    reconnectDelay: 3000,
    currentUrl: null,
    listenerIdCounter: 0,
    _closeTimeout: null,

    // Register a new listener (Blazor component). Returns a unique listener ID.
    connect: function (url, dotNetRef) {
        const listenerId = ++this.listenerIdCounter;
        this.listeners.set(listenerId, dotNetRef);
        
        // Cancel any pending close timeout
        if (this._closeTimeout) {
            clearTimeout(this._closeTimeout);
            this._closeTimeout = null;
        }
        
        // If already connected to same URL, just notify the new listener
        if (this.socket && this.socket.readyState === WebSocket.OPEN && this.currentUrl === url) {
            dotNetRef.invokeMethodAsync('OnConnected');
            return listenerId;
        }
        
        // If socket exists but to different URL or not open, close it first
        if (this.socket) {
            this.socket.close();
        }
        
        this.currentUrl = url;
        this.reconnectAttempts = 0;
        this._connect(url);
        
        return listenerId;
    },

    _connect: function (url) {
        try {
            this.socket = new WebSocket(url);

            this.socket.onopen = () => {
                this.reconnectAttempts = 0;
                this._notifyAllListeners('OnConnected');
            };

            this.socket.onmessage = (event) => {
                try {
                    const message = JSON.parse(event.data);
                    this._handleMessage(message);
                } catch (e) {
                    console.error('Failed to parse WebSocket message:', e);
                }
            };

            this.socket.onclose = (event) => {
                this._notifyAllListenersWithArg('OnDisconnected', event.reason || 'Connection closed');

                // Attempt reconnection only if we still have listeners
                if (this.listeners.size > 0 && this.reconnectAttempts < this.maxReconnectAttempts) {
                    this.reconnectAttempts++;
                    setTimeout(() => this._connect(url), this.reconnectDelay);
                }
            };

            this.socket.onerror = (error) => {
                console.error('WebSocket error:', error);
            };
        } catch (e) {
            console.error('Failed to connect WebSocket:', e);
            this._notifyAllListenersWithArg('OnDisconnected', 'Failed to connect');
        }
    },

    _notifyAllListeners: function (methodName) {
        for (const [id, dotNetRef] of this.listeners) {
            try {
                dotNetRef.invokeMethodAsync(methodName);
            } catch (e) {
                // Listener may have been disposed, remove it
                this.listeners.delete(id);
            }
        }
    },

    _notifyAllListenersWithArg: function (methodName, arg) {
        for (const [id, dotNetRef] of this.listeners) {
            try {
                dotNetRef.invokeMethodAsync(methodName, arg);
            } catch (e) {
                // Listener may have been disposed, remove it
                this.listeners.delete(id);
            }
        }
    },

    _handleMessage: function (message) {
        // Broadcast to ALL listeners
        for (const [id, dotNetRef] of this.listeners) {
            try {
                this._sendToListener(dotNetRef, message);
            } catch (e) {
                // Listener may have been disposed, remove it
                this.listeners.delete(id);
            }
        }
    },

    _sendToListener: function (dotNetRef, message) {
        switch (message.eventType) {
            case 'OrderAccepted':
                const acceptedPayload = message.payload;
                dotNetRef.invokeMethodAsync(
                    'OnOrderAccepted',
                    acceptedPayload.orderId,
                    acceptedPayload.partnerName || '',
                    acceptedPayload.estimatedMinutes || 0,
                    acceptedPayload.timestamp || ''
                );
                break;

            case 'OrderRejected':
                const rejectedPayload = message.payload;
                dotNetRef.invokeMethodAsync(
                    'OnOrderRejected',
                    rejectedPayload.orderId,
                    rejectedPayload.reason || '',
                    rejectedPayload.timestamp || ''
                );
                break;

            case 'OrderReady':
                const readyPayload = message.payload;
                dotNetRef.invokeMethodAsync(
                    'OnOrderReady',
                    readyPayload.orderId,
                    readyPayload.partnerName || '',
                    readyPayload.timestamp || ''
                );
                break;

            case 'OrderPickedUp':
                const pickedUpPayload = message.payload;
                dotNetRef.invokeMethodAsync(
                    'OnOrderPickedUp',
                    pickedUpPayload.orderId,
                    pickedUpPayload.agentName || '',
                    pickedUpPayload.timestamp || ''
                );
                break;

            case 'OrderDelivered':
                const deliveredPayload = message.payload;
                dotNetRef.invokeMethodAsync(
                    'OnOrderDelivered',
                    deliveredPayload.orderId,
                    deliveredPayload.timestamp || ''
                );
                break;

            default:
                console.log('Unknown event type:', message.eventType);
                break;
        }
    },

    // Remove a specific listener by ID (called when Blazor component disposes)
    removeListener: function (listenerId) {
        this.listeners.delete(listenerId);
        
        // Don't close socket immediately - another page might register soon (Blazor navigation)
        // Only close if no listeners after a delay
        if (this.listeners.size === 0) {
            if (this._closeTimeout) {
                clearTimeout(this._closeTimeout);
            }
            this._closeTimeout = setTimeout(() => {
                if (this.listeners.size === 0 && this.socket) {
                    this.socket.close();
                    this.socket = null;
                    this.currentUrl = null;
                }
            }, 2000); // 2 second grace period for page navigation
        }
    },

    // Legacy disconnect - removes all listeners and closes socket
    disconnect: function () {
        this.listeners.clear();
        if (this.socket) {
            this.socket.close();
            this.socket = null;
        }
        this.currentUrl = null;
    }
};
