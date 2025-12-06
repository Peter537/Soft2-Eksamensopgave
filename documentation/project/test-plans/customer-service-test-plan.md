# **Testplan for MToGo's Customer Service**

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

**CustomerService** fungerer som adapter/facade til **LegacyMToGo** og sikrer oprettelse, opdatering og søgning af kundekonti uden datatab eller afvigelser fra legacy systemet.

**Modenhedsniveau:** Level 1 (Foundation) - Se [project-test-plan.md](project-test-plan.md#level-1-foundation-services)

Projektet skal:

- Validere integrationen mod LegacyMToGoApp
- Verificere data-output i forhold til legacy-systemet
- Bekræfte forretningsinvarianter i den nye serviceopsætning

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
4. LegacyMToGo er tilgængelig for integrationstests

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### Customer-specifikke Risici

| Risk ID | Risk | Mitigation | Severity | Likelihood |
|:--------|:-----|:-----------|:---------|:-----------|
| R7.6 | LegacyMToGo API ændringer bryder CustomerService adapter | Implementer contract tests mod Legacy API; overvåg for breaking changes; test med mocked Legacy responses | Significant | Possible |

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [project-test-plan.md](project-test-plan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**
- Legacy API adapter tests (mock LegacyMToGo responses)

Servicen indgår i de system-wide **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [project-test-plan.md](project-test-plan.md#værktøjer).

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
