# **Testplan for MToGo's Agent Bonus Service**

Version: 1.0  
Teststrategi-version: 1.0  
Security-plan-version: 1.0  
Project-test-plan-version: 1.0  
Dato: 5. december 2025  
Forfattere: Oskar, Peter og Yusuf

## **Indholdsfortegnelse**

- [Indledning](#indledning)
- [Risikoanalyse](#risikoanalyse)
- [Test fremgangsmåde](#test-fremgangsmåde)
- [Test deliverables](#test-deliverables)

## **Indledning**

**AgentBonusService** håndterer beregning af bonusser til Agents baseret på deres performance. Servicen aggregerer data fra andre services via Gateway.

**Modenhedsniveau:** Level 4 (Aggregation & Support) - Se [project-test-plan.md](project-test-plan.md#level-4-aggregation--support-services)

Projektet skal:

- Validere at AgentBonusServicen korrekt processerer data
- Verificere at AgentBonusServicen outputter korrekt data
- Bekræfte at alle forretnings invarianter fungerer korrekt

### Målgruppe

- **Udvikler**: Implementerer og vedligeholder tests/kvalitet
- **QA Manager/Test Lead**: Udfører og overvåger tests, validerer test deliverables
- **DevOps**: Opsætter miljøer og pipelines
- **Security Expert**: Gennemfører sikkerhedsvurderinger og validerer sikkerhedstests
- **Product Owner / Arkitekt**: Validerer acceptance tests
- **Compliance Officer**: Sikrer overholdelse af databeskyttelsesregler

### Assumptions

1. Stabilt udviklingsmiljø og lokal testopsætning
2. Testdata er tilgængeligt eller kan genereres (ingen produktionsdata)
3. API-kontrakter ændres ikke under testplanen
4. Gateway og bagvedliggende services (Agent, Order) er tilgængelige for integrationstests

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### AgentBonus-specifikke Risici

| Risk ID | Risk                                                      | Mitigation                                                                                                                                       | Severity    | Likelihood |
| :------ | :-------------------------------------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R7.7    | Gateway API ændringer bryder AgentBonusService            | Implementer contract tests mod Gateway; mock Gateway responses i unit tests; køre integrationstest med real Gateway                              | Significant | Possible   |
| R10.8   | Forkert bonus-beregning giver agents for meget/lidt bonus | Implementer omfattende unit tests med boundary analysis; validér beregningslogik i code review; tilføj acceptance tests for alle bonus-scenarier | Critical    | Possible   |

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [project-test-plan.md](project-test-plan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**

Denne service er en **undtagelse** fra den generelle arkitektur, da den faktisk kalder andre services via Gateway (HTTP). Dette kræver:

- **Gateway Integration Tests**: Test af samspil mellem AgentBonusService og Gateway med real HTTP calls
- **Mock-baseret Unit Tests**: For isoleret test af bonus-beregningslogik
- **Boundary Value Analysis**: For at validere beregningslogik ved grænseværdier

Servicen indgår i de system-wide **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [project-test-plan.md](project-test-plan.md#værktøjer).

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
