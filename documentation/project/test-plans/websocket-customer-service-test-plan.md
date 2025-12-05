# **Testplan for MToGo's WebSocket Customer Service**

Version: 1.0  
Teststrategi-version: 1.0  
Security-plan-version: 1.0  
Projekt-testplan-version: 1.0  
Dato: 5. december 2025  
Forfattere: Oskar, Peter og Yusuf

## **Indholdsfortegnelse**

- [Indledning](#indledning)
- [Risikoanalyse](#risikoanalyse)
- [Test fremgangsmåde](#test-fremgangsmåde)
- [Test deliverables](#test-deliverables)

## **Indledning**

**WebSocketCustomerService** fungerer som en realtidskommunikationstjeneste, der videresender Kafka-begivenheder til kunder via WebSocket-forbindelser.

**Modenhedsniveau:** Level 3 (Event Consumers) - Se [projekt-testplan.md](projekt-testplan.md#level-3-event-consumers)

Projektet skal:

- Validere at WebSocket-forbindelser etableres og vedligeholdes korrekt
- Verificere at order events forwardes med det samme og pålideligt til de korrekte kunder
- Bekræfte at connection management (connect, disconnect, reconnect) fungerer robust
- Sikre korrekt routing af events til rigtige kunder baseret på kunde ID
- Teste at simultane connections fra flere kunder håndteres effektivt

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

### WebSocket Customer-specifikke Risici

| Risk ID | Risk | Mitigation | Severity | Likelihood |
|:--------|:-----|:-----------|:---------|:-----------|
| R1.10 | WebSocket connection overload ved mange simultane kunder | Implementer connection pooling; load test med realistisk antal connections; implementer graceful degradation | Significant | Possible |
| R6.8 | WebSocket connections droppes ved netværksproblemer | Implementer auto-reconnect logik i client; test reconnection scenarios; implementer heartbeat/ping-pong | Moderate | Likely |

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [projekt-testplan.md](projekt-testplan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**
- Kafka consumer tests med real Kafka container
- WebSocket connection management tests
- Multi-client broadcast tests

**Kafka Topics Konsumeret:**

| Topic | Handling |
|-------|----------|
| `order-accepted` | Notificér kunde om ordre accepteret |
| `agent-assigned` | Notificér kunde om agent tildelt |
| `order-ready` | Notificér kunde om ordre klar til afhentning |
| `order-pickedup` | Notificér kunde om ordre afhentet |
| `order-delivered` | Notificér kunde om ordre leveret |

Servicen indgår i de system-wide **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [projekt-testplan.md](projekt-testplan.md#værktøjer).

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
