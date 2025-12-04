# MToGo WebSocket Customer Service

Microservice der leverer realtids WebSocket-forbindelser til kunder.

## Formål

WebSocket Customer Service muliggør realtidskommunikation med kunder:

- **Ordreopdateringer**: Push ordrestatusændringer til kunder
- **Live Tracking**: Realtids leveringssporingsopdateringer
- **Notifikationer**: Øjeblikkelige notifikationer for ordreevents

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **WebSockets**: SignalR / native WebSocket
- **Messaging**: Apache Kafka consumer

## WebSocket Events

Events pushet til forbundne kunder:

| Event            | Beskrivelse                 |
| ---------------- | --------------------------- |
| `OrderAccepted`  | Ordre accepteret af partner |
| `OrderRejected`  | Ordre afvist af partner     |
| `OrderReady`     | Ordre klar til afhentning   |
| `OrderPickedUp`  | Agent afhentede ordren      |
| `OrderDelivered` | Ordre leveret succesfuldt   |

## Forbindelse

Kunder forbinder via: `/api/v1/ws/customers`

Autentificering kræves via JWT token.
