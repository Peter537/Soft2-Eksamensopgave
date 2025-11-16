# Kafka Order Demo - Microservices Architecture

Event-driven food ordering system built with .NET, Kafka, and Kubernetes.

## Architecture

### Services
- **CentralHub** - API Gateway (routes requests, no business logic)
- **OrderService** - Order management & database operations
- **PartnerService** - Restaurant partner operations (accept/reject orders)
- **WebsocketPartnerService** - Real-time WebSocket updates to restaurant UIs
- **NotificationService** - Customer notifications (email/SMS)
- **LocationService** - GPS tracking (delivery simulation)
- **Frontend** - Blazor UI

### Event Flow
```
Customer → Gateway → OrderService (DB) → Kafka Events
                                            ↓
                          ┌─────────────────┴───────────────┐
                          ▼                                 ▼
                    NotificationService          WebsocketPartnerService
                    (Customer alerts)            (Restaurant real-time UI)
```

### Key Features
✅ **API Gateway Pattern** - CentralHub has no business logic  
✅ **Event-Driven** - Kafka for async communication  
✅ **No OrderPreparing** - Implicit when accepted  
✅ **WebSocket Ready** - Placeholder for real-time partner updates  

## Quick Start

### Local Development
```powershell
# Start Kafka
docker-compose up kafka -d

# Run services (separate terminals)
cd CentralHub.API; dotnet run      # Port 5000
cd OrderService; dotnet run         # Port 5001
cd PartnerService; dotnet run       # Port 5002
cd WebsocketPartnerService; dotnet run  # Port 5003
cd NotificationService; dotnet run  # Port 5004
cd KafkaOrderDemo; dotnet run       # Frontend
```

### Docker Compose
```powershell
docker-compose up --build
```

### Kubernetes
```powershell
# Deploy all services
.\k8s-deploy.ps1

# Teardown
.\k8s-teardown.ps1
```

## Kubernetes Structure

All manifests in `k8s/` directory:
- `00-namespace.yaml` - Creates `kafka-demo` namespace
- `01-kafka.yaml` - Kafka broker (StatefulSet)
- `02-centralhub.yaml` - API Gateway
- `03-partnerservice.yaml` - Partner operations
- `04-locationservice.yaml` - GPS tracking
- `05-notificationservice.yaml` - Customer notifications
- `06-frontend.yaml` - Blazor UI

Services communicate via Kafka event bus within cluster.

## Documentation
- **ARCHITECTURE.md** - Detailed architecture & flow diagrams
- **TESTING.md** - Testing guide & verification steps
- **WebsocketPartnerService/README.md** - WebSocket implementation guide
- **KUBERNETES.md** - K8s deployment details

## Tech Stack
- **.NET 8.0** - All microservices
- **Apache Kafka** - Event bus
- **Blazor** - Frontend UI
- **Docker** - Containerization
- **Kubernetes** - Orchestration