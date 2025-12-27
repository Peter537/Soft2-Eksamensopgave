# **Testplan for MToGo's Order Service**

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

**OrderService** er kernefunktionalitet i **MToGo** og sikrer at en Customer kan bruge en app til at bestille mad som en Local Agent bringer ud til kunden. Denne service orkestrerer hele ordreflowet og publicerer Kafka events der driver resten af platformen.

**Modenhedsniveau:** Level 2 (Core Flow) - Se [project-test-plan.md](project-test-plan.md#level-2-core-order-flow)

Projektet skal:

- Validere at Order Servicen korrekt processerer og persisterer data
- Verificere at Order Servicen outputter korrekt data
- Bekræfte at alle forretnings invarianter fungerer korrekt
- Sikre løs kobling for skalering fra 3,6 millioner til 18 millioner ordrer

### Målgruppe

- **Udvikler**: Implementerer og vedligeholder tests/kvalitet
- **QA Manager/Test Lead**: Udfører og overvåger tests, validerer test deliverables
- **DevOps**: Opsætter miljøer, pipelines og performance tests
- **Security Expert**: Gennemfører sikkerhedsvurderinger og validerer sikkerhedstests
- **Product Owner / Arkitekt**: Validerer acceptance tests
- **Compliance Officer**: Sikrer overholdelse af databeskyttelsesregler

### Assumptions

1. Stabilt udviklingsmiljø og lokal testopsætning
2. Testdata er tilgængeligt eller kan genereres (ingen produktionsdata)
3. API-kontrakter og event-schemaer ændres ikke under testplanen
4. Kafka kører stabilt i testmiljøet

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### Order-specifikke Risici

| Risk ID | Risk                                                                                                        | Mitigation                                                                                                                                | Severity | Likelihood |
| :------ | :---------------------------------------------------------------------------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------- | :------- | :--------- |
| R9.3    | Race conditions: Samme ordre assignes til flere agents pga. concurrent requests                             | Brug pessimistic concurrency control med database locking for assignment; test med multi-threaded scenarios; stress test assignment logic | Severe   | Likely     |
| R10.6   | [OrderAggregate](../business/domain-driven-design.md#orderaggregate) business invariants fra DDD overtrædes | Test alle business rules i OrderAggregate via BDD og mutation testing                                                                     | Severe   | Possible   |
| R10.7   | Invalid order states eller transitions ikke håndteret korrekt                                               | Test alle state transitions; test invalid transitions er rejected; strikt opsættelse af state transitions; brug state machine pattern     | Moderate | Possible   |

## **Test fremgangsmåde**

### Testtilgang

Vi anvender **BDD (Behavior-Driven Development)** til Controllers og Service-laget med business-kritisk logik, for at sikre fælles forståelse mellem stakeholders og udviklere via Gherkin-scenarier. Til resten af koden bruger vi en Test-After tilgang.

### Scope

Standard testtyper anvendes. Se [project-test-plan.md](project-test-plan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**

| Test Type                      | Beskrivelse                                   | Formål                                  |
| ------------------------------ | --------------------------------------------- | --------------------------------------- |
| BDD Tests (Reqnroll)           | Gherkin-scenarier for order state transitions | Fælles forståelse af business logic     |
| Mutation Testing (Stryker.NET) | Test af test-kvalitet for OrderAggregate      | Sikre robuste tests af kritisk logik    |
| Kafka Producer Tests           | Verificer events publiceres korrekt           | Sikre downstream services modtager data |

**Kafka Topics Produceret:**

| Topic             | Event               | Trigger               |
| ----------------- | ------------------- | --------------------- |
| `order-created`   | OrderCreatedEvent   | Kunde opretter ordre  |
| `order-accepted`  | OrderAcceptedEvent  | Partner accepterer    |
| `order-rejected`  | OrderRejectedEvent  | Partner afviser       |
| `agent-assigned`  | AgentAssignedEvent  | Agent tager ordre     |
| `order-ready`     | OrderReadyEvent     | Partner markerer klar |
| `order-pickedup`  | OrderPickedUpEvent  | Agent afhenter        |
| `order-delivered` | OrderDeliveredEvent | Agent leverer         |

### Værktøjer

Standard værktøjer anvendes. Se [project-test-plan.md](project-test-plan.md#værktøjer).

**Service-specifikke tilføjelser:**

- **Reqnroll**: BDD framework for Gherkin-scenarier
- **Stryker.NET**: Mutation testing for OrderAggregate
- **NBomber**: Performance testing af order creation flow

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Mutation test report (Stryker.NET)
- BDD test scenarios (Gherkin)
- Static code analysis
- Performance report
