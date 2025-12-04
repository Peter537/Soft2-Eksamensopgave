# MToGo WebSocket Agent Service

Microservice der leverer realtids WebSocket-forbindelser til leveringsagenter.

## Formål

WebSocket Agent Service muliggør realtidskommunikation med leveringsagenter:

- **Ordrenotifikationer**: Push nye ordretildelinger til agenter
- **Ordreopdateringer**: Realtids ordrestatusændringer
- **Klar til Afhentning**: Notificer når ordrer er klar til afhentning

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **WebSockets**: SignalR / native WebSocket
- **Messaging**: Apache Kafka consumer

## WebSocket Events

Events pushet til forbundne agenter:

| Event              | Beskrivelse                  |
| ------------------ | ---------------------------- |
| `OrderAccepted`    | Ordre accepteret af partner  |
| `OrderReady`       | Ordre klar til afhentning    |
| `AgentAssigned`    | Agent tildelt til en ordre   |
| `DeliveryAccepted` | Levering accepteret af agent |

## Forbindelse

Agenter forbinder via to endpoints:

| Endpoint                      | Beskrivelse                          |
| ----------------------------- | ------------------------------------ |
| `/api/v1/ws/agents`           | Broadcast til alle forbundne agenter |
| `/api/v1/ws/agents/{agentId}` | Personligt rum for specifik agent    |

Autentificering kræves via JWT token.
