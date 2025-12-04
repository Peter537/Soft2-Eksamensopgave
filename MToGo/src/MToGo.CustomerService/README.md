# MToGo Customer Service

Microservice ansvarlig for kundeautentificering og profiladministration.

## Formål

Customer Service håndterer alle kunde-relaterede operationer:

- **Autentificering**: Kunde-login og JWT token-generering
- **Registrering**: Oprettelse af nye kundekonti (via Legacy MToGo)
- **Profiladministration**: Opdatering af kundeprofiler

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Autentificering**: JWT Bearer tokens
- **Integration**: Legacy MToGo API

## API Endpoints

| Metode | Endpoint                  | Beskrivelse         |
| ------ | ------------------------- | ------------------- |
| POST   | `/api/v1/customers`       | Registrer ny kunde  |
| POST   | `/api/v1/customers/login` | Kunde-login         |
| GET    | `/api/v1/customers/{id}`  | Hent kundeprofil    |
| PATCH  | `/api/v1/customers/{id}`  | Opdater kundeprofil |
| DELETE | `/api/v1/customers/{id}`  | Slet kundekonto     |

## Integration

Denne service integrerer med Legacy MToGo-applikationen for kundedata-persistens.
