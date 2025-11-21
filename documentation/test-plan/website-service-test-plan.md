# **Testplan for MToGo's Website Service**

Version: 1.0  
Teststrategi-version: 1.0  
Security-plan-version: 1.0  
Dato: 21. november 2025  
Forfattere: Oskar, Peter og Yusuf

## **Indholdsfortegnelse**

- [Indledning](#indledning)
- [Risikoanalyse](#risikoanalyse)
- [Test fremgangsmåde](#test-fremgangsmåde)
- [Tidsplan og prioriteringsliste](#tidsplan-og-prioriteringsliste)
- [Exit Criteria](#exit-criteria)
- [Test deliverables](#test-deliverables)

## **Indledning**

**WebsiteService** fungerer som frontend-service for **MToGo** platformen, den håndterer brugerinteraktioner via webapplikationen og kommunikerer med backend API services samt WebSockets for realtidsopdateringer.

Projektet skal:

- Validere at Website Servicen korrekt håndterer brugerinteraktioner.
- Verificere at Website Servicen korrekt kommunikerer med backend services.
- Bekræfte at WebSocket-forbindelser fungerer korrekt for realtidsdata.

### Målgruppe

- **Udvikler**: Implementerer og vedligeholder tests/kvalitet.
- **QA Manager/Test Lead**: Udfører og overvåger tests, validerer test deliverables.
- **DevOps**: Opsætter miljøer og pipelines.
- **Security Expert**: Gennemfører sikkerhedsvurderinger og validerer sikkerhedstests.
- **Product Owner / Arkitekt / Designer (UX)**: Validerer acceptance tests.
- **Compliance Officer**: Sikrer overholdelse af databeskyttelsesregler.

### Assumptions

Dette er de forudsætninger, vi antager er sande for at denne testplan kan være gyldig og gennemføres:

1. Stabilt udviklingsmiljø og lokal testopsætning.
2. Testdata er tilgængeligt eller kan genereres (ingen produktionsdata).
3. API-kontrakter og event-schemaer ændres ikke under testplanen.
4. WebSockets kører stabilt i testmiljøet.

### Entry Criteria

- Teststrategi og security plan godkendt.
- CI/CD pipeline opsat med testdatabaser.
- DDD-model godkendt.
- Generelle produkt risici identificeret.
- Preliminary arkitekturdesign.
- Branch protection-regler opsat.
- Figma Wireframe godkendt.

## **Risikoanalyse**

Denne sektion identificerer risici for denne service baseret på de 10 parent-risici fra teststrategien.

### Shared Risks Oversigt

Denne sektion refererer til de shared risks defineret i [shared-risks.md](shared-risks.md), hvor de relevante shared-risks for denne service er:

- R1 (Performance & Skalering): R1.1-R1.7 kan ses i [shared-risks.md](shared-risks.md#performance--skalering-parent-r1)
- R2 (Sikkerhed & Compliance): R2.1-R2.5 kan ses i [shared-risks.md](shared-risks.md#sikkerhed--compliance-parent-r2)
- R3 (Sikkerhed & Compliance): R3.1-R3.5 kan ses i [shared-risks.md](shared-risks.md#sikkerhed--compliance-parent-r3)
- R4 (Team & Ressource): R4.1-R4.7 kan ses i [shared-risks.md](shared-risks.md#team--ressource-parent-r4)
- R5 (Udvikling & Kvalitet): R5.1-R5.3 kan ses i [shared-risks.md](shared-risks.md#udvikling--kvalitet-parent-r5)
- R6 (Infrastruktur & Miljø): R6.1-R6.6 kan ses i [shared-risks.md](shared-risks.md#infrastruktur--miljø-parent-r6)
- R7 (Infrastruktur & Miljø): R7.1-R7.5 kan ses i [shared-risks.md](shared-risks.md#infrastruktur--miljø-parent-r7)
- R8 (Integration & Deployment): R8.1-R8.5 kan ses i [shared-risks.md](shared-risks.md#integration--deployment-parent-r8)
- R9 (Integration & Deployment): R9.1-R9.2 kan ses i [shared-risks.md](shared-risks.md#integration--deployment-parent-r9)
- R10 (Udvikling & Kvalitet): R10.1-R10.5 kan ses i [shared-risks.md](shared-risks.md#udvikling--kvalitet-parent-r10)

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Omfanget inkluderer følgende testtyper, tilpasset fra strategien til denne service:

- **Unit-tests**: Individuelle komponenter. Størrelse: Én funktionalitet.
- **Integrationstests**: Samspil mellem WebsiteServicen og gateway.
- **Acceptance Tests**: Tester UI/UX og WebSocket-forbindelser.
- **Specificationsbaserede tests**: Baseret på user stories.
- **Sikkerhedstests**: OWASP-checks.

Servicen indgår derudover i de system-wide **system tests** og **end-to-end tests**, hvor hele platformens flow valideres samlet.

### Code Coverage krav

| Metrik | Controller | Service-layer | Repository | Øvrige komponenter |
| ------ | ---------- | ------------- | ---------- | ------------------ |
| Line   | >70%       | >70%          | >70%       | >60%               |
| Branch | >70%       | >70%          | >70%       | >60%               |
| Method | >70%       | >70%          | >70%       | >60%               |

### Værktøjer

- xUnit: Test framework
- Moq: Mocks
- Coverlet & reportgenerator: Code coverage
- StyleCop & SonarQube: Static analysis
- GitHub Actions: CI/CD-pipeline
- Docker: Container
- Selenium: UI tests

## **Tidsplan og prioriteringsliste**

1. Unit tests og specifikationsbaserede tests
2. Acceptance tests
3. Integrationstests
4. Sikkerhedstests

## **Exit Criteria**

- Code Coverage krav er opfyldt
- Alle prioriteringer er opnået
- Intet kritisk fra sikkerheds scan
- Alle tests skal være succesfulde
- Alle user stories og funktionelle stories er testet

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
