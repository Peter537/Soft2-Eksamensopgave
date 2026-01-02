# MToGo Order Service

Microservice ansvarlig for ordreoprettelse, -administration og livscyklussporing.

## Formål

Order Service håndterer alle ordre-relaterede operationer:

- **Ordreoprettelse**: Opret nye ordrer med varer og leveringsdetaljer
- **Ordreadministration**: Opdater ordrestatus gennem livscyklussen
- **Ordresporing**: Spor ordrefremdrift fra oprettelse til levering
- **Event-publicering**: Publicer ordre-events til Kafka

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL
- **Messaging**: Apache Kafka
- **Metrics**: Prometheus

## Ordre Livscyklus

```
Placed → Accepted → Ready → PickedUp → Delivered
       ↘ Rejected
```

## API Endpoints

| Metode | Endpoint                                      | Beskrivelse                    |
| ------ | --------------------------------------------- | ------------------------------ |
| POST   | `/api/v1/orders/order`                        | Opret en ny ordre              |
| GET    | `/api/v1/orders/order/{id}`                   | Hent ordre efter ID            |
| POST   | `/api/v1/orders/order/{id}/accept`            | Accepter ordre (partner)       |
| POST   | `/api/v1/orders/order/{id}/reject`            | Afvis ordre (partner)          |
| POST   | `/api/v1/orders/order/{id}/set-ready`         | Marker ordre som klar          |
| POST   | `/api/v1/orders/order/{id}/assign-agent`      | Tildel agent til ordre         |
| POST   | `/api/v1/orders/order/{id}/pickup`            | Agent afhenter ordre           |
| POST   | `/api/v1/orders/order/{id}/complete-delivery` | Fuldfør levering               |
| GET    | `/api/v1/orders/customer/{id}`                | Hent ordrer for kunde          |
| GET    | `/api/v1/orders/customer/{id}/active`         | Hent aktive ordrer for kunde   |
| GET    | `/api/v1/orders/agent/{id}`                   | Hent ordrer for agent          |
| GET    | `/api/v1/orders/agent/{id}/active`            | Hent aktive ordrer for agent   |
| GET    | `/api/v1/orders/partner/{id}`                 | Hent ordrer for partner        |
| GET    | `/api/v1/orders/partner/{id}/active`          | Hent aktive ordrer for partner |
| GET    | `/api/v1/orders/available`                    | Hent tilgængelige jobs (agent) |

## Kafka Topics

| Topic             | Beskrivelse                 |
| ----------------- | --------------------------- |
| `order-created`   | Ny ordre oprettet           |
| `order-accepted`  | Ordre accepteret af partner |
| `order-rejected`  | Ordre afvist af partner     |
| `agent-assigned`  | Agent tildelt til ordre     |
| `order-ready`     | Ordre klar til afhentning   |
| `order-pickedup`  | Ordre afhentet af agent     |
| `order-delivered` | Ordre leveret til kunde     |
