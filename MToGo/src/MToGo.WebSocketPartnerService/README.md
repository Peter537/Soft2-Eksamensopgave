# MToGo WebSocket Partner Service

Microservice der leverer realtids WebSocket-forbindelser til partnere (restauranter).

## Formål

WebSocket Partner Service muliggør realtidskommunikation med partnere:

- **Nye Ordrer**: Push nye ordrenotifikationer til partnere
- **Agenttildeling**: Notificer når en agent er tildelt
- **Ordreopdateringer**: Realtids ordrestatusændringer

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **WebSockets**: SignalR / native WebSocket
- **Messaging**: Apache Kafka consumer

## WebSocket Events

Events pushet til forbundne partnere:

| Event           | Beskrivelse                      |
| --------------- | -------------------------------- |
| `OrderCreated`  | Ny ordre modtaget                |
| `AgentAssigned` | Leveringsagent tildelt til ordre |
| `OrderPickedUp` | Ordre afhentet af agent          |

## Forbindelse

Partnere forbinder via: `/api/v1/ws/partners`

Autentificering kræves via JWT token.
