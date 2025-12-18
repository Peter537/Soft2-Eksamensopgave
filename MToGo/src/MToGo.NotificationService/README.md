# MToGo Notification Service

Microservice ansvarlig for håndtering af notifikationer via Kafka event-forbrug.

## Formål

Notification Service forbruger Kafka-events og udsender notifikationer:

- **Event-forbrug**: Lyt til ordre- og systemevents fra Kafka
- **Notifikationsudsendelse**: Send notifikationer via Legacy MToGo
- **Eventbehandling**: Behandl og rout events til relevante handlers

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Messaging**: Apache Kafka consumer
- **Integration**: Legacy MToGo notifikations-API

## Kafka Topics

Servicen lytter til følgende Kafka topics:

| Topic             | Beskrivelse                 |
| ----------------- | --------------------------- |
| `order-accepted`  | Ordre accepteret af partner |
| `order-rejected`  | Ordre afvist af partner     |
| `order-pickedup`  | Ordre afhentet af agent     |
| `order-delivered` | Ordre leveret til kunde     |

## API Endpoint

| Metode | Endpoint                  | Beskrivelse               |
| ------ | ------------------------- | ------------------------- |
| POST   | `/api/v1/notifications`   | Send notifikation manuelt |

## Integration

Notifikationer udsendes gennem Legacy MToGo-applikationens notifikations-API.
