# SQL

Denne mappe indeholder SQL-scripts der bruges til lokal drift af MToGo via Docker Compose og Terraform.

## Struktur

| Mappe              | Formål                                                                       |
| ------------------ | ---------------------------------------------------------------------------- |
| [init/](./init/)   | Init-scripts som køres automatisk af Postgres-containeren ved første opstart |
| [seeds/](./seeds/) | Demo seed-data som kan indlæses valgfrit                                     |

## Init (databaser)

Filen [init/01-create-databases.sql](./init/01-create-databases.sql) opretter de service-specifikke databaser (fx `mtogo_orders`, `mtogo_agents`, `mtogo_legacy`, osv.).

Bemærk: Scriptet bruger `psql`-features (`\gexec`) og er derfor målrettet den officielle `postgres` Docker image init-mekanisme.

## Demo seed (valgfrit)

Filen [seeds/mtogo_demo_seed.sql](./seeds/mtogo_demo_seed.sql) indeholder repeatable demo-data, som bruges til at gøre platformen nem at demonstrere lokalt.

Demo seeding styres via miljøvariablen `MTOGO_SEED_DEMO_DATA` (se `.env.example`). Når den er sat til `true`, kører Docker Compose en seed-proces der venter på at tabellerne findes og indlæser seed-data.

Anbefalet: kør seeding via Docker Compose, så rækkefølge og afhængigheder håndteres korrekt.

Det er også muligt at køre det til Terraform baserede opsætninger via deploy.ps1-scriptet i terraform-mappen ved at bruge `-SeedDemoData` flaget.
