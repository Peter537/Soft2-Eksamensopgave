# MToGo Shared

Delt bibliotek der indeholder fælles modeller, DTOs, utilities og udvidelser brugt på tværs af alle MToGo-services.

## Formål

Det delte bibliotek indeholder:

- **Modeller**: Fælles entity-modeller og enums
- **DTOs**: Data Transfer Objects til API-kommunikation
- **Extensions**: Nyttige extension methods
- **Utilities**: Fælles hjælpeklasser
- **Konstanter**: Delte konstanter og konfigurationsnøgler

## Indhold

| Mappe       | Beskrivelse                              |
| ----------- | ---------------------------------------- |
| Models/     | Delte entity-modeller og enums           |
| DTOs/       | Request og response DTOs                 |
| Extensions/ | Extension methods til fælles operationer |
| Constants/  | Delte konstanter                         |

## Brug

Referer til dette projekt i andre MToGo-services:

```xml
<ProjectReference Include="..\MToGo.Shared\MToGo.Shared.csproj" />
```

## NuGet Pakker

Fælles pakker brugt på tværs af services er defineret her og refereret via `Directory.Packages.props` for centraliseret versionsstyring.
