# **Testplan for MToGo's Website Service**

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

**WebsiteService** fungerer som frontend-service for **MToGo** platformen, den håndterer brugerinteraktioner via webapplikationen og kommunikerer med backend API services samt WebSockets for realtidsopdateringer.

**Modenhedsniveau:** Level 5 (System Maturity) - Se [project-test-plan.md](project-test-plan.md#level-5-system-maturity)

Projektet skal:

- Validere at Website Servicen korrekt håndterer brugerinteraktioner
- Verificere at Website Servicen korrekt kommunikerer med backend services
- Bekræfte at WebSocket-forbindelser fungerer korrekt for realtidsdata

### Målgruppe

- **Udvikler**: Implementerer og vedligeholder tests/kvalitet
- **QA Manager/Test Lead**: Udfører og overvåger tests, validerer test deliverables
- **DevOps**: Opsætter miljøer og pipelines
- **Security Expert**: Gennemfører sikkerhedsvurderinger og validerer sikkerhedstests
- **Product Owner / Arkitekt / Designer (UX)**: Validerer acceptance tests
- **Compliance Officer**: Sikrer overholdelse af databeskyttelsesregler

### Assumptions

1. Stabilt udviklingsmiljø og lokal testopsætning
2. Testdata er tilgængeligt eller kan genereres (ingen produktionsdata)
3. API-kontrakter og event-schemaer ændres ikke under testplanen
4. WebSockets kører stabilt i testmiljøet
5. Figma Wireframe er godkendt

## **Risikoanalyse**

### Shared Risks

Shared risks fra [shared-risks.md](shared-risks.md) gælder for denne service (R1-R10).

### Website-specifikke Risici

| Risk ID | Risk                                                      | Mitigation                                                                                           | Severity | Likelihood |
| :------ | :-------------------------------------------------------- | :--------------------------------------------------------------------------------------------------- | :------- | :--------- |
| R1.8    | Browser-inkompatibilitet fører til dårlig brugeroplevelse | Implementer cross-browser testing (Chrome, Firefox, Safari, Edge); brug moderne CSS/JS med fallbacks | Moderate | Possible   |
| R2.6    | XSS/CSRF sårbarheder i frontend kode                      | Implementer Content Security Policy; validér input; brug anti-CSRF tokens; kør OWASP ZAP scans       | Critical | Unlikely   |

## **Test fremgangsmåde**

### Testtilgang

Test-After tilgangen anvendes for at sikre hurtig levering af funktionalitet, efterfulgt af målrettede tests for at validere robusthed og kompatibilitet.

### Scope

Standard testtyper anvendes. Se [project-test-plan.md](project-test-plan.md#test-typer) for definitioner.

**Service-specifikke tilføjelser:**

- UI/UX tests via Selenium
- Cross-browser compatibility tests
- WebSocket connection tests
- Visual regression tests (optional)

Website indgår som central del af **E2E tests** på Level 5, hvor hele platformens flow valideres samlet.

### Værktøjer

Standard værktøjer anvendes. Se [project-test-plan.md](project-test-plan.md#standard-værktøjer).

**Service-specifikke tilføjelser:**

- **Selenium**: UI automation og cross-browser testing
- **Playwright** (optional): Moderne browser automation alternative

## **Test deliverables**

- Code Coverage report
- Test summary
- Security summary
- Static code analysis
- UI/UX test report
