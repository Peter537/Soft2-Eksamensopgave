# MToGo Management Service

Microservice ansvarlig for administrative funktioner og kontoadministration.

## Formål

Management Service giver administrative kapabiliteter:

- **Admin-autentificering**: Management-login med sikre legitimationsoplysninger
- **Kontooprettelse**: Opret nye partner- og agentkonti
- **Platformoversigt**: Adgang til platformdækkende data og metrikker

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL
- **Autentificering**: JWT Bearer tokens med management-rolle

## API Endpoints

| Metode | Endpoint                      | Beskrivelse           |
| ------ | ----------------------------- | --------------------- |
| POST   | `/api/v1/management/login`    | Management-login      |
| POST   | `/api/v1/management/agents`   | Opret ny agentkonto   |
| POST   | `/api/v1/management/partners` | Opret ny partnerkonto |

## Sikkerhed

Denne service kræver management-legitimationsoplysninger konfigureret via miljøvariabler:

- `Management__Username`
- `Management__Password`
