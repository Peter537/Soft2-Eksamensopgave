# Soft2-Eksamensopgave

## Gruppe

- Oskar (Ossi-1337, cph-oo221)
- Peter (Peter537, cph-pa153)
- Yusuf (StylizedAce, cph-ya56)

## MToGo projektet

MToGo er en madleveringsplatform bygget med en microservices-arkitektur, der forbinder kunder med restauranter og leveringsagenter.

Koden til projektet findes i [MToGo/src/](./MToGo/src/) mappen.

### Oversigt

Platformen muliggør:

- **Kunder** kan gennemse restauranter, afgive ordrer og spore leveringer i realtid
- **Partnere** (restauranter) kan administrere menuer, acceptere ordrer og opdatere ordrestatus
- **Agenter** (leveringschauffører) kan modtage ordrer, opdatere leveringsstatus og optjene bonusser
- **Management** kan overvåge platformen, oprette partner/agent konti og se analyser

### Arkitektur

MToGo er bygget som en serviceorienteret arkitektur med:

- **REST APIs** gennem en central Gateway (YARP)
- **WebSockets** til realtidsopdateringer
- **Kafka** til event-drevet kommunikation
- **PostgreSQL** til datapersistens
- **Prometheus & Grafana** til overvågning

## Repository-struktur

| Mappe                              | Beskrivelse                                       |
| ---------------------------------- | ------------------------------------------------- |
| [.github/](./.github/)             | GitHub Actions workflows og issue-skabeloner      |
| [documentation/](./documentation/) | Projektdokumentation og arkitektur                |
| [LegacyMToGo/](./LegacyMToGo/)     | Legacy-applikation til kunder & notifikationer    |
| [MToGo/](./MToGo/)                 | Hovedapplikation med alle microservices           |
| [monitoring/](./monitoring/)       | Prometheus, Grafana og Alertmanager konfiguration |
| [terraform/](./terraform/)         | Infrastructure as Code                            |

## Hurtig Start

Dette er en hurtig guide til at køre MToGo-platformen lokalt ved hjælp af Docker Compose.

En større guide kan findes i [documentation/deliverables/deployment-guide.md](./documentation/deliverables/deployment-guide.md)

### Forudsætninger

- Docker & Docker Compose
- .NET 8 SDK (til lokal udvikling)

### Kørsel med Docker Compose

1. Kopier eksempel-miljøfilen:

   ```bash
   cp .env.example .env
   ```

2. Konfigurer `.env`-filen med dine indstillinger

3. Start alle services:
   ```bash
   docker compose up -d --build
   ```

### Tjenester

| Service      | URL                   |
| ------------ | --------------------- |
| Website      | http://localhost:8081 |
| API Gateway  | http://localhost:8080 |
| Legacy API   | http://localhost:8082 |
| Prometheus   | http://localhost:9090 |
| Grafana      | http://localhost:3000 |
| Alertmanager | http://localhost:9093 |

## Dokumentation

Se [documentation/](./documentation/)-mappen for at finde dokumentation omkring:

- [deliverables/](./documentation/deliverables/) som indeholder projektets leverancer
- [internal-team/](./documentation/internal-team/) som indeholder dokumentation til internt brug
- [project/](./documentation/project/) som indeholder projektets overordnede dokumentation og arkitektur

## Udvikling

Alle tests kan findes i [MToGo/tests/](./MToGo/tests/) mappen.

### Kørsel af Tests

```bash
cd MToGo
dotnet test MToGo.sln
```

### Byg Individuelle Services

```bash
cd MToGo/src/MToGo.OrderService
dotnet run
```

## Teknologi Stack

- **Backend**: ASP.NET Core 8.0, C#
- **Frontend**: Blazor Server
- **Database**: PostgreSQL
- **Messaging**: Apache Kafka
- **Gateway**: YARP (Yet Another Reverse Proxy)
- **Overvågning**: Prometheus, Grafana, Alertmanager
- **Containerisering**: Docker, Docker Compose
