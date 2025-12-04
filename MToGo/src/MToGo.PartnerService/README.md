# MToGo Partner Service

Microservice ansvarlig for restaurant/partner-administration og menuadministration.

## Formål

Partner Service håndterer alle partner-relaterede operationer:

- **Autentificering**: Partner-login og JWT token-generering
- **Menuadministration**: Opret, opdater og slet menupunkter
- **Partnerprofiler**: Partnerinformation og indstillinger
- **Tilgængelighed**: Spor partnertilgængelighedsstatus

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL
- **Autentificering**: JWT Bearer tokens
- **Metrics**: Prometheus

## API Endpoints

### Partner Endpoints

| Metode | Endpoint                       | Beskrivelse                |
| ------ | ------------------------------ | -------------------------- |
| POST   | `/api/v1/partners`             | Registrer ny partner       |
| POST   | `/api/v1/partners/login`       | Partner-login              |
| GET    | `/api/v1/partners`             | List alle partnere         |
| GET    | `/api/v1/partners/{id}`        | Hent partner efter ID      |
| PATCH  | `/api/v1/partners/{id}`        | Opdater partneroplysninger |
| PATCH  | `/api/v1/partners/{id}/active` | Skift partner aktiv-status |

### Menu Endpoints

| Metode | Endpoint                                       | Beskrivelse              |
| ------ | ---------------------------------------------- | ------------------------ |
| GET    | `/api/v1/partners/{partnerId}/menu-items`      | Hent alle menupunkter    |
| GET    | `/api/v1/partners/{partnerId}/menu-items/{id}` | Hent specifikt menupunkt |
| POST   | `/api/v1/partners/{partnerId}/menu-items`      | Opret nyt menupunkt      |
| PATCH  | `/api/v1/partners/{partnerId}/menu-items/{id}` | Opdater menupunkt        |
| DELETE | `/api/v1/partners/{partnerId}/menu-items/{id}` | Slet menupunkt           |
