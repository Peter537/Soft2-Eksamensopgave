# How to Run the Kafka Order Demo

This guide will walk you through getting the entire system up and running on your local machine using Kubernetes.

## Prerequisites

### 1. Install Docker Desktop

Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop/) for Windows.

### 2. Enable Kubernetes in Docker Desktop

1. Open **Docker Desktop**
2. Click the **Settings** icon (gear icon) in the top-right
3. Go to **Kubernetes** in the left sidebar
4. Check the box: **"Enable Kubernetes"**
5. Click **Apply & Restart**
6. Wait for the Kubernetes status indicator to turn green (this may take a few minutes)

That's it! You now have a local Kubernetes cluster running.

## Running the Application

### 1. Open PowerShell

Navigate to the project folder:
```powershell
cd C:\Users\yusef\Documents\GitHub\Soft2-Eksamensopgave
```

### 2. Deploy Everything

Run the deployment script:
```powershell
.\k8s-deploy.ps1
```

**First time running?** This will take 2-5 minutes as it:
- Builds Docker images for all services
- Deploys Kafka
- Deploys all microservices (CentralHub, OrderService, PartnerService, etc.)
- Sets up networking

**Subsequent runs?** Only 2-3 minutes (uses cached Docker layers).

You'll see a success message when everything is ready:
```
================================================================
         DEPLOYMENT COMPLETE!
================================================================
```

### 3. Access the Application

Open your web browser and go to:

**http://localhost:30080**

This opens the main application with three pages:

## Using the Application

### 🍕 Customer View (Home Page)
**URL:** http://localhost:30080

1. Select a pizza and drink
2. Enter your name and delivery address
3. Click **"Proceed to Payment →"**
4. In the payment popup, click **"Pay Now"**
5. Your order is created and sent to the restaurant!

### 🍳 Restaurant View (Kitchen Page)
**URL:** http://localhost:30080/kitchen

This is what the restaurant sees on their iPad:

1. New orders appear with a **yellow "Pending"** badge
2. Click an order to see details
3. Click **"Accept Order"** or **"Reject Order"**
4. Once accepted, click **"Mark as Ready"** when food is cooked
5. Click **"Mark as Picked Up"** when the driver collects the order

### 🚚 Driver View (Delivery Page)
**URL:** http://localhost:30080/delivery

This is what the delivery driver sees:

1. Orders ready for delivery appear here
2. Click **"Mark as Delivered"**
3. Click **"Take Photo"** (mocked - just a simulation)
4. Click **"Confirm Delivery"**
5. Done! The customer gets notified.

## Behind the Scenes

While you're using the app, the system is:
- **Publishing Kafka events** for each status change
- **Sending notifications** to customers via NotificationService
- **Tracking GPS location** (simulated) when orders are picked up
- **Routing all requests** through the CentralHub API Gateway

### View Live Logs

Want to see what's happening behind the scenes? Open another PowerShell window and run:

```powershell
# Watch the gateway
kubectl logs -f deployment/centralhub -n kafka-demo

# Watch order creation
kubectl logs -f deployment/orderservice -n kafka-demo

# Watch restaurant actions
kubectl logs -f deployment/partnerservice -n kafka-demo

# Watch customer notifications
kubectl logs -f deployment/notificationservice -n kafka-demo

# Watch GPS tracking
kubectl logs -f deployment/locationservice -n kafka-demo
```

You'll see beautiful console output showing exactly what's happening in real-time!

## Stopping the Application

When you're done, shut everything down:

```powershell
.\k8s-teardown.ps1
```

This removes all deployments and frees up resources.

## Making Changes

If you modify the code:

1. **Tear down** the current deployment:
   ```powershell
   .\k8s-teardown.ps1
   ```

2. **Make your changes** to the code

3. **Redeploy**:
   ```powershell
   .\k8s-deploy.ps1
   ```

The script will rebuild only the services you changed.

## Troubleshooting

### Pods not starting?

Check the status:
```powershell
kubectl get pods -n kafka-demo
```

All pods should show `Running` status.

### Port already in use?

Make sure no other applications are using ports 30001-30080. Close them and redeploy.

### Kafka connection errors?

Give it a minute - Kafka takes ~30 seconds to fully start. The other services will retry and connect automatically.

### Still having issues?

Check the full logs:
```powershell
kubectl get pods -n kafka-demo
kubectl logs <pod-name> -n kafka-demo
```

## Architecture Overview

The system consists of:

- **Frontend (Blazor)** - The web UI you interact with
- **CentralHub** - API Gateway (routes all requests, no business logic)
- **OrderService** - Creates orders, saves to database, publishes Kafka events
- **PartnerService** - Handles restaurant actions (accept/reject/ready/pickup/delivered)
- **WebsocketPartnerService** - Will handle real-time WebSocket updates to restaurant (placeholder for now)
- **NotificationService** - Sends customer notifications
- **LocationService** - GPS tracking simulation
- **Kafka** - Event streaming platform

Everything communicates through Kafka events, creating a fully event-driven microservices architecture!

---

**That's it! You're now running a production-like Kubernetes deployment on your local machine. 🎉**
