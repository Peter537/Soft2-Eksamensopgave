# MToGo.LogCollectorService

## Beskrivelse

Log Collector Service er ansvarlig for at samle og opbevare logs fra alle services i MToGo-platformen. Servicen lytter på `app-logs` Kafka topicet og behandler logs baseret på deres type:

- **Audit Logs**: Gemmes i PostgreSQL database til søgning, compliance og sporbarhed
- **System Logs**: Gemmes i daglige roterende filer (`system-yyyy-MM-dd.log`) til debugging og overvågning

## Teknologier

- **ASP.NET Core 8.0**: Web framework
- **Entity Framework Core**: ORM for PostgreSQL
- **Apache Kafka**: Event streaming for log indsamling
- **PostgreSQL**: Database til audit logs
- **File System**: Daglige roterende log filer

## API Endpoints

> **Note:** Alle endpoints kræver Management-rolle autorisation.

### Audit Logs

| Endpoint                      | Metode | Beskrivelse                                    |
| :---------------------------- | :----- | :--------------------------------------------- |
| `/api/v1/logs/audit`          | GET    | Henter audit logs med pagination og filtrering |
| `/api/v1/logs/audit/services` | GET    | Henter liste over unikke service navne         |
| `/api/v1/logs/audit/actions`  | GET    | Henter liste over unikke actions               |

**Query Parameters for GET /api/v1/logs/audit:**

| Parameter     | Type   | Beskrivelse                                                   |
| :------------ | :----- | :------------------------------------------------------------ |
| `page`        | int    | Side nummer (default: 1)                                      |
| `pageSize`    | int    | Antal logs per side (default: 50, max: 100)                   |
| `serviceName` | string | Filtrer på service navn                                       |
| `level`       | string | Filtrer på log niveau (Information, Warning, Error, Critical) |
| `action`      | string | Filtrer på action (f.eks. OrderCreated, LoginAttempt)         |
| `resource`    | string | Filtrer på resource type (f.eks. Order, Customer)             |
| `userId`      | string | Filtrer på bruger ID                                          |
| `fromDate`    | date   | Start dato (YYYY-MM-DD)                                       |
| `toDate`      | date   | Slut dato (YYYY-MM-DD)                                        |
| `search`      | string | Søg i beskeder                                                |

### System Logs

| Endpoint                           | Metode | Beskrivelse                              |
| :--------------------------------- | :----- | :--------------------------------------- |
| `/api/v1/logs/system/files`        | GET    | Henter liste over tilgængelige log filer |
| `/api/v1/logs/system/files/{date}` | GET    | Henter indhold af en specifik log fil    |

## Konfiguration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mtogo_logs;Username=mtogo;Password=mtogo_password"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "log-collector-group"
  },
  "Logging": {
    "LogDirectory": "/app/logs"
  }
}
```

### Environment Variables

| Variable                               | Beskrivelse                  |
| :------------------------------------- | :--------------------------- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Kafka__BootstrapServers`              | Kafka broker adresse         |
| `Kafka__GroupId`                       | Kafka consumer group ID      |
| `Logging__LogDirectory`                | Mappe til system log filer   |

## Kafka Topics

### Lytter på

| Topic      | Beskrivelse                                |
| :--------- | :----------------------------------------- |
| `app-logs` | Modtager alle logs fra services i systemet |

### Log Entry Format

```json
{
  "id": "guid",
  "timestamp": "2024-01-15T10:30:00Z",
  "logType": 0,
  "level": "Information",
  "serviceName": "OrderService",
  "category": "MToGo.OrderService.Services.OrderService",
  "message": "[AUDIT] OrderCreated: Order created...",
  "data": "{\"orderId\": 123}",
  "userId": "42",
  "correlationId": "abc-123",
  "machineName": "order-service-pod-1"
}
```

## Database Schema

### AuditLog Table

| Kolonne       | Type         | Beskrivelse                  |
| :------------ | :----------- | :--------------------------- |
| Id            | bigint       | Primary key (auto-increment) |
| Timestamp     | timestamp    | Tidspunkt for log entry      |
| Level         | varchar(50)  | Log niveau                   |
| ServiceName   | varchar(100) | Navn på service              |
| Category      | varchar(500) | Logger category              |
| Message       | text         | Log besked                   |
| Data          | text         | Ekstra data (JSON)           |
| UserId        | varchar(100) | Bruger ID                    |
| CorrelationId | varchar(100) | Korrelations ID              |
| MachineName   | varchar(100) | Maskine navn                 |

## Brug fra Andre Services

For at sende logs til Log Collector Service, skal du bruge `MToGo.Shared` logging modulet:

```csharp
// I Program.cs
using MToGo.Shared.Logging;

builder.Services.AddKafkaLogging("ServiceName", LogLevel.Information);

// For audit logs, brug AuditLoggerExtensions
logger.LogAuditInformation(
    action: "OrderCreated",
    resource: "Order",
    resourceId: orderId.ToString(),
    userId: customerId,
    userRole: "Customer",
    message: "Order created successfully"
);
```

## Links

- [MToGo.Shared Logging](../MToGo.Shared/Logging/)
