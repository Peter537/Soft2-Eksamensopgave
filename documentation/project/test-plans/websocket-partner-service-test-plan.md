# **Testplan for MToGo's WebSocket Partner Service**

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

**WebSocketPartnerService** fungerer som en realtidskommunikationstjeneste, der videresender Kafka-begivenheder til restaurantpartnere via WebSocket-forbindelser.

**Modenhedsniveau:** Level 3 (Event Consumers) - Se [project-test-plan.md](project-test-plan.md#level-3-event-consumers)

Projektet skal:

- Validere at WebSocket-forbindelser etableres og vedligeholdes korrekt
- Verificere at order events forwardes med det samme og pålideligt til de korrekte partnere
- Bekræfte at connection management (connect, disconnect, reconnect) fungerer robust
- Sikre korrekt routing af events til rigtige restauranter baseret på restaurant ID
- Teste at simultane connections fra flere partnere håndteres effektivt

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
3. API-kontrakter og event-schemaer ændres ikke under testplanen

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### WebSocket Partner-specifikke Risici

| Risk ID | Risk | Mitigation | Severity | Likelihood |
|:--------|:-----|:-----------|:---------|:-----------|
| R1.11 | WebSocket connection overload ved mange simultane partnere | Implementer connection pooling; load test med realistisk antal connections; implementer graceful degradation | Significant | Possible |
| R6.9 | WebSocket connections droppes ved netværksproblemer | Implementer auto-reconnect logik i client; test reconnection scenarios; implementer heartbeat/ping-pong | Moderate | Likely |

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [project-test-plan.md](project-test-plan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**
- Kafka consumer tests med real Kafka container
- WebSocket connection management tests
- Multi-client broadcast tests

**Kafka Topics Konsumeret:**

| Topic | Handling |
|-------|----------|
| `order-created` | Notificér restaurant om ny ordre |
| `agent-assigned` | Notificér restaurant om agent på vej |
| `order-pickedup` | Notificér restaurant om ordre afhentet |

Servicen indgår i de system-wide **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [project-test-plan.md](project-test-plan.md#værktøjer).

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
