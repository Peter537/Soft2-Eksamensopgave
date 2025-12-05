# **Testplan for MToGo's Feedback Hub Service**

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

**Feedback Hub Servicen** er en service i systemet som tager imod reviews fra kunder efter levering af mad.

**Modenhedsniveau:** Level 4 (Aggregation & Support) - Se [projekt-testplan.md](projekt-testplan.md#level-4-aggregation--support-services)

Projektet skal:

- Validere at Feedback Hub Servicen korrekt processerer og persisterer data
- Verificere at Feedback Hub Servicen outputter korrekt data
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

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### FeedbackHub-specifikke Risici

Ingen service-specifikke risici identificeret udover shared risks.

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [projekt-testplan.md](projekt-testplan.md#test-typer) for definitioner.

Servicen indgår i de system-wide **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [projekt-testplan.md](projekt-testplan.md#værktøjer).

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
