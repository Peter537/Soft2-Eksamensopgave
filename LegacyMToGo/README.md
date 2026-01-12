# Legacy MToGo

Dette er den gamle MToGo-applikation, der håndterer kundeadministration og notifikationer.

## Teknologi Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL
- **Container**: Docker

## Projektstruktur

| Mappe/Fil    | Beskrivelse                                  |
| ------------ | -------------------------------------------- |
| Controllers/ | API-controllere til kunder og notifikationer |
| Entities/    | Database-kontekst og entity-modeller         |
| Models/      | Request og response modeller                 |
| Services/    | Forretningslogik-services                    |
| Program.cs   | Applikationens indgangspunkt                 |

## Kørsel

### Med Docker Compose (anbefalet)

Når hele platformen køres via Docker Compose fra repository root, eksponeres Legacy API på:

- http://localhost:8082

Se [README.md](../README.md) for samlet opsætning.

### Lokalt (kun denne service)

Kør fra mappen [LegacyMToGo/](./):

```powershell
dotnet run --project .\LegacyMToGo.csproj
```

Bemærk: Port/URL styres af dine lokale launch settings og/eller environment variables; i Docker kører servicen på port 8080 internt og mappes til 8082 på host.
