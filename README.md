# Soft2-Eksamensopgave

## Gruppe

- Oskar (Ossi-1337, cph-oo221)
- Peter (Peter537, cph-pa153)
- Yusuf (StylizedAce, cph-ya56)

## GitHub Projects

Vores projektstyring kan findes her:  
[https://github.com/users/Peter537/projects/15](https://github.com/users/Peter537/projects/15)

## MToGo projektet

MToGo er en madleveringsplatform bygget med en service-orienteret arkitektur, der forbinder kunder med restauranter og leveringsagenter.

### Oversigt

Platformen muliggør:

- **Kunder** kan gennemse restauranter, afgive ordrer og spore leveringer i real time
- **Partnere** (restauranter) kan administrere menuer, acceptere ordrer og opdatere ordrestatus
- **Agenter** (kurerer) kan modtage ordrer, opdatere leveringsstatus og optjene bonusser
- **Management** kan overvåge platformen, oprette partner/agent konti og se analyser

### Arkitektur

MToGo er bygget med:

- **REST APIs** gennem en central Gateway (YARP)
- **WebSockets** til realtime opdateringer
- **Kafka** til event-drevet kommunikation
- **PostgreSQL** til datapersistens
- **Prometheus & Grafana** til overvågning

## Hurtig Start

Dette er en hurtig guide til at køre MToGo-platformen lokalt ved hjælp af Docker Compose.

En mere detaljeret beskrivelse af hvordan man kører og udvikler til systemet kan findes i [/documentation/deliverables/deployment-guide.md](./documentation/deliverables/deployment-guide.md).

### Forudsætninger

- Docker & Docker Compose kører på din maskine
- .NET 8 SDK (til lokal udvikling)

### Kørsel af Tests

For at køre tests lokalt:

```bash
cd MToGo
dotnet test
```

Dette vil køre alle tests i programmet og du kan se testresultaterne.

### Kørsel med Docker Compose

1. Kopier eksempel-miljøfilen:

   ```bash
   cp .env.example .env
   ```

   Hvis du bruger PowerShell (Windows):

   ```powershell
   Copy-Item .env.example .env
   ```

2. Konfigurer `.env`-filen med dine værdier

   Hvis `DISCORD_WEBHOOK_ALERT` ikke er opdateret, vil Alertmanager ikke sende notifikationer, men i stedet logge fejl i konsollen.

   Hvis du ønsker at der kommer demo-data i databaserne ved opstart, kan du sætte `MTOGO_SEED_DEMO_DATA` til `true`.

3. Start alle services:
   ```bash
   docker compose up -d --build
   ```

### Tjenester

| Service       | URL                   |
| ------------- | --------------------- |
| Website       | http://localhost:8081 |
| API Gateway   | http://localhost:8080 |
| Legacy API    | http://localhost:8082 |
| Prometheus    | http://localhost:9090 |
| Grafana       | http://localhost:3000 |
| Grafana (SLO) | http://localhost:3001 |
| Alertmanager  | http://localhost:9093 |

## Repository-struktur

| Mappe                                                      | Beskrivelse                                                                                                                                                                                                                                                                               |
| ---------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [.config/](./.config/)                                     | Konfigurationsfiler til dotnet tools                                                                                                                                                                                                                                                      |
| [.github/](./.github/)                                     | GitHub Actions workflows og issue-skabeloner                                                                                                                                                                                                                                              |
| [documentation/](./documentation/)                         | - [deliverables/](./documentation/deliverables/) indeholder projektets leverancer<br>- [internal-team/](./documentation/internal-team/) indeholder dokumentation til internt brug<br>- [project/](./documentation/project/) indeholder projektets overordnede dokumentation og arkitektur |
| [LegacyMToGo/](./LegacyMToGo/)                             | Legacy-applikation til kunder & notifikationer                                                                                                                                                                                                                                            |
| [MToGo/](./MToGo/)                                         | Hovedapplikation med alle services.<br>Koden til servicesne ligger i [MToGo/src/](./MToGo/src/)<br>Tests til programmet ligger i [MToGo/tests/](./MToGo/tests/)                                                                                                                           |
| [monitoring/](./monitoring/)                               | Prometheus, Grafana og Alertmanager konfiguration til KPI'er                                                                                                                                                                                                                              |
| [monitoring-infrastructure/](./monitoring-infrastructure/) | Yderligere overvågningsinfrastruktur og dashboards til SLO'er                                                                                                                                                                                                                             |
| [sql/](./sql/)                                             | Database initialisering scripts og demo data                                                                                                                                                                                                                                              |
| [terraform/](./terraform/)                                 | Infrastructure as Code med Terraform til deployment                                                                                                                                                                                                                                       |
