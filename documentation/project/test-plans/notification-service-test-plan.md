# **Testplan for MToGo's Notification Service**

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

**NotificationService** er adapteren, der sikrer at alle events fra **Kafka** bliver omsat til korrekte notifikationer som skal sendes til **LegacyMToGoApp** uden datatab eller afvigelser fra legacy systemet.

**Modenhedsniveau:** Level 3 (Event Consumers) - Se [project-test-plan.md](project-test-plan.md#level-3-event-consumers)

Projektet skal:

- Validere integrationen mod LegacyMToGoApp og Kafka
- Verificere at beskedpayloads er identiske med legacy-format

### Målgruppe

- **Udvikler**: Implementerer og vedligeholder tests/kvalitet
- **QA Manager/Test Lead**: Udfører og overvåger tests, validerer test deliverables
- **DevOps**: Opsætter miljøer og pipelines
- **Security Expert**: Gennemfører sikkerhedsvurderinger og validerer sikkerhedstests
- **Product Owner / Arkitekt**: Validerer acceptance tests
- **Compliance Officer**: Sikrer overholdelse af databeskyttelsesregler

### Assumptions

1. Stabilt udviklingsmiljø og lokal testopsætning
2. Testdata kan genereres uden brug af produktionsdata
3. API-kontrakter, Kafka topics og event schemas er stabile
4. LegacyMToGo er tilgængelige i testmiljøet

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### Notification-specifikke Risici

| Risk ID | Risk                                                              | Mitigation                                                                                                | Severity    | Likelihood |
| :------ | :---------------------------------------------------------------- | :-------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R7.6    | LegacyMToGo notification API ændringer bryder NotificationService | Implementer contract tests mod Legacy API; overvåg for breaking changes; test med mocked Legacy responses | Significant | Possible   |

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [project-test-plan.md](project-test-plan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**

- Kafka consumer tests med real Kafka container
- Legacy API adapter tests (mock LegacyMToGo responses)

**Kafka Topics Konsumeret:**

| Topic             | Handling                    |
| ----------------- | --------------------------- |
| `order-accepted`  | Send notification til kunde |
| `agent-assigned`  | Send notification til kunde |
| `order-delivered` | Send notification til kunde |

Servicen indgår i de system-wide **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [project-test-plan.md](project-test-plan.md#standard-værktøjer).

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
