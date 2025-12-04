# Workflows Documentation

Denne mappe indeholder dokumentation for udviklingsworkflows.

## Dokumenter

| Dokument                               | Beskrivelse                       |
| -------------------------------------- | --------------------------------- |
| [pipelines.md](./pipelines.md)         | CI/CD pipelines og automatisering |
| [security-plan.md](./security-plan.md) | Sikkerhedsplan og -praksis        |

## Formål

Denne dokumentation beskriver:

- Hvordan kode bygges og deployes automatisk
- Sikkerhedspraksis og -procedurer
- DevOps workflows

## CI/CD Pipeline

Projektet bruger GitHub Actions til:

- Automatisk build ved push
- Kørsel af unit tests
- Kørsel af integration tests
- Docker image builds
- Deployment til miljøer

## Sikkerhed

Sikkerhedsplanen dækker:

- Authentication og authorization
- Data beskyttelse
- Sårbarhedshåndtering
- Compliance krav
