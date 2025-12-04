# MToGo Gateway

API Gateway-service der bruger YARP (Yet Another Reverse Proxy) til at route alle eksterne requests til interne microservices.

## Formål

Gateway er det eneste indgangspunkt for alle eksterne requests til MToGo-platformen. Den håndterer:

- **Request Routing**: Router requests til de relevante microservices
- **Autentificering**: JWT token-validering
- **CORS**: Cross-origin resource sharing-politikker
- **Load Balancing**: Fordeler trafik på tværs af service-instanser

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Proxy**: YARP (Yet Another Reverse Proxy)
- **Autentificering**: JWT Bearer tokens

## Endpoints

Alle API-endpoints proxies gennem Gateway:

| Route Pattern                | Mål Service                |
| ---------------------------- | -------------------------- |
| `/api/v1/orders/*`           | Order Service              |
| `/api/v1/customers/*`        | Customer Service           |
| `/api/v1/legacy/customers/*` | Legacy MToGo               |
| `/api/v1/partners/*`         | Partner Service            |
| `/api/v1/agents/*`           | Agent Service              |
| `/api/v1/agent-bonus/*`      | Agent Bonus Service        |
| `/api/v1/feedback-hub/*`     | Feedback Hub Service       |
| `/api/v1/notifications/*`    | Legacy MToGo               |
| `/api/v1/management/*`       | Management Service         |
| `/api/v1/ws/customers/*`     | WebSocket Customer Service |
| `/api/v1/ws/partners/*`      | WebSocket Partner Service  |
| `/api/v1/ws/agents/*`        | WebSocket Agent Service    |

## Kørsel

```bash
dotnet run
```

**Port**: `8080`
