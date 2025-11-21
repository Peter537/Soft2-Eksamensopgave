# **Testplan for MToGo's Order Service**

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

**OrderService** er kernefunktionalitet i **MToGo** og den sikrer at en Customer kan bruge en app til at bestille mad som en Local Agent bringer ud til kunden.

Projektet skal:

- Validere at Order Servicen korrekt processere og persisterer data.
- Verificere at Order Servicen outputter korrekt data.
- Bekræfte at alle forretnings invarianter fungere korrekt.
- Der er løs kobling for skalering fra 3,6 millioner til 18 millioner ordrer.

### Målgruppe

- **Udvikler**: Implementerer og vedligeholder tests/kvalitet.
- **QA Manager/Test Lead**: Udfører og overvåger tests, validerer test deliverables.
- **DevOps**: Opsætter miljøer, pipelines og performance tests.
- **Security Expert**: Gennemfører sikkerhedsvurderinger og validerer sikkerhedstests.
- **Product Owner / Arkitekt**: Validerer acceptance tests.
- **Compliance Officer**: Sikrer overholdelse af databeskyttelsesregler.

### Assumptions

Dette er de forudsætninger, vi antager er sande for at denne testplan kan være gyldig og gennemføres:

1. Stabilt udviklingsmiljø og lokal testopsætning.
2. Testdata er tilgængeligt eller kan genereres (ingen produktionsdata).
3. API-kontrakter og event-schemaer ændres ikke under testplanen.
4. Kafka kører stabilt i testmiljøet.

### Entry Criteria

- Teststrategi og security plan godkendt.
- CI/CD pipeline opsat med testdatabaser.
- DDD-model godkendt.
- Generelle produkt risici identificeret.
- Preliminary arkitekturdesign.
- Branch protection-regler opsat.

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

### Order-specifikke risici

Dette er en liste over de risici, der er specifikke for OrderService, som er i forlængelse af de shared risks:

| Risk ID | Risk                                                                                               | Mitigation                                                                                                                                                                                        | Severity | Likelihood |
| :------ | :------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :------- | :--------- |
| R9.3    | Race conditions: Samme ordre assignes til flere agents pga. concurrent requests                    | Brug pessimistic concurrency control med database locking for assignment; test med multi-threaded scenarios; stress test assignment logic                                                         | Severe   | Likely     |
| R10.6   | [OrderAggregate](../domain-driven-design.md#orderaggregate) business invariants fra DDD overtrædes | Test alle business rules i OrderAggregate                                                                                                                                                         | Severe   | Possible   |
| R10.7   | Invalid order states eller transitions ikke håndteret korrekt                                      | Test alle state transitions; test invalid transitions er rejected; strikt opsættelse af state transitions (states kan kun ændres i definerede trin, og ikke på tværs); brug state machine pattern | Moderate | Possible   |

## **Test fremgangsmåde**

### Testtilgang

Vi anvender BDD (Behavior-Driven Development) til Controllers og Service-laget med business-kritisk logik, for at sikre fælles forståelse mellem stakeholders og udviklere via Gherkin-scenarier. Til resten af koden bruger vi en Test-After tilgang for at opretholde hurtig og effektiv testdækning.

### Scope

Omfanget inkluderer følgende testtyper, tilpasset fra strategien til denne service:

- **Unit-tests**: Individuelle komponenter. Størrelse: Én metode.
- **Integrationstests**: Samspil mellem OrderServicen, gateway og databasen.
- **Acceptance Tests**: Tester API'er og Kafka Producere.
- **Specificationsbaserede tests**: Baseret på user stories.
- **Sikkerhedstests**: OWASP-checks.
- **System Tests**: Smoke tests.

Udover ovenstående anvendes mutation testing, begrænset til kernefunktionalitet på grund af omkostninger.

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
- JMeter: Performance
- StyleCop & SonarQube: Static analysis
- Reqnroll: BDD
- GitHub Actions: CI/CD-pipeline
- Docker: Container
- Stryker.NET: Mutationtest

## **Tidsplan og prioriteringsliste**

1. Unit tests og specifikationsbaserede tests
2. Acceptance tests
3. Integrationstests
4. Sikkerhedstests
5. System tests
6. Mutation testing

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
- Mutation test report
- Static code analysis
- Performance report
