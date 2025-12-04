# MToGo Agent Service

Microservice ansvarlig for leveringsagent-administration og tilgængelighedssporing.

## Formål

Agent Service håndterer alle agent-relaterede operationer:

- **Autentificering**: Agent-login og JWT token-generering
- **Tilgængelighed**: Spor og skift agenttilgængelighedsstatus
- **Leveringstildeling**: Tildel agenter til ordrer
- **Agentprofiler**: Agentinformationsadministration

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL
- **Autentificering**: JWT Bearer tokens
- **Metrics**: Prometheus

## API Endpoints

| Metode | Endpoint                     | Beskrivelse              |
| ------ | ---------------------------- | ------------------------ |
| POST   | `/api/v1/agents`             | Registrer ny agent       |
| POST   | `/api/v1/agents/login`       | Agent-login              |
| GET    | `/api/v1/agents/{id}`        | Hent agent efter ID      |
| PATCH  | `/api/v1/agents/{id}`        | Opdater agentoplysninger |
| PATCH  | `/api/v1/agents/{id}/active` | Skift agent aktiv-status |
| DELETE | `/api/v1/agents/{id}`        | Slet agent               |
