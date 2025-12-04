# Shared Risks - Fælles Risici for MToGo's Projekter

**Version:** 1.0  
**Teststrategi-version:** 1.0  
**Date:** 18. November 2025  
**Author(s):** Oskar, Peter og Yusuf

## Indledning

Dette dokument indeholder detaljerede risici, der er fælles for projekterne til produktet for MToGo. Disse risici kan refereres direkte i individuelle testplaner for at undgå gentagelse og sikre konsistent håndtering på tværs af projekter.

Hver risiko er knyttet til en parent-risiko fra IT-konsulenthusets teststrategi (R1-R10) og udfolder denne i specifikke sub-risici (R1.1, R1.2, R1.3...) med konkrete mitigationsstrategier.

Dette dokument opdateres løbende baseret på erfaringer fra projekter. Testplaner kan referere til disse risici og tilføje projektspecifikke detaljer eller tilpasninger.

**Anvendelse**: Testplaner skal kun referere til disse risici (f.eks. "R1.1: Se SharedRisks R1.1") og skal ikke gentage detaljerne. Kun projektspecifikke risici, der ikke findes i dette dokument, skal skrives fuldt ud i testplanen med fortsættende nummerering (f.eks. hvis R1 har 7 shared risici, starter projektspecifikke risici ved R1.8).

**Struktur**: Risici er organiseret efter kategori og parent-risiko fra IT-konsulenthusets teststrategi. Hver risiko har Risk ID, beskrivelse, mitigation, severity og likelihood.

## Risikoanalyse

### Performance & Skalering (Parent: R1)

| Risk ID | Risk                                                                          | Mitigation                                                                                                                      | Severity    | Likelihood |
| :------ | :---------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------ | :---------- | :--------- |
| R1.1    | Høj belastning under peak perioder fører til langsom responstid eller timeout | Implementer load testing i CI/CD-pipeline; brug autoskalering; inkluder performance tests i regression suite                    | Significant | Likely     |
| R1.2    | Database queries bliver for langsomme under høj belastning                    | Optimér queries med indexes; anvend caching strategier; overvåg query performance; test med realistisk data volume              | Significant | Possible   |
| R1.3    | API rate limiting eller throttling forårsager request failures                | Implementer graceful degradation; test rate limiting scenarios; overvåg API usage patterns; implementer retry logic med backoff | Moderate    | Possible   |
| R1.4    | Memory leaks i langtidskørende processer                                      | Brug profiling tools; automatiser resource monitoring i containers                                                              | Moderate    | Possible   |
| R1.5    | Connection pool exhaustion til database eller message queues                  | Konfigurer connection pooling korrekt; implementer connection timeout og limits; overvåg pool usage metrics                     | Significant | Possible   |
| R1.6    | Thread starvation eller deadlocks i concurrent scenarios                      | Brug async/await patterns korrekt; test multi-threaded scenarios; implementer timeout mechanisms; review concurrent code        | Moderate    | Possible   |
| R1.7    | Kapacitetsbegrænsninger i testmiljø påvirker performance test validitet       | Plan for skalérbare testmiljøer; simulér production-like load; dokumentér miljø kapacitet og begrænsninger                      | Moderate    | Possible   |

### Sikkerhed & Compliance (Parent: R2)

| Risk ID | Risk                                           | Mitigation                                                                                                                         | Severity    | Likelihood |
| :------ | :--------------------------------------------- | :--------------------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R2.1    | SQL Injection i input-validering               | Anvend parameterized queries eller ORM; kør OWASP tests; validér alle inputs                                                       | Severe      | Possible   |
| R2.2    | XSS (Cross-Site Scripting) attacks             | Implementer input sanitization og output encoding; brug Content Security Policy headers; test med XSS payloads                     | Significant | Possible   |
| R2.3    | Uautoriseret adgang via API endpoints          | Implementer JWT-authentication og role-based access control; test authorization logic; brug API rate limiting                      | Severe      | Unlikely   |
| R2.4    | Broken authentication eller session management | Implementer secure session handling; brug HTTPS; test session timeout og token invalidation                                        | Severe      | Possible   |
| R2.5    | Sårbarheder i tredjeparts biblioteker          | Brug dependency-check; opdatér dependencies regelmæssigt; overvåg security advisories; Udvikl in-house biblioteker hvor nødvendigt | Significant | Possible   |

### Sikkerhed & Compliance (Parent: R3)

| Risk ID | Risk                                                          | Mitigation                                                                                                              | Severity    | Likelihood |
| :------ | :------------------------------------------------------------ | :---------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R3.1    | Manglende data encryption                                     | Kryptér sensitive data i database (GDPR krav); test encryption implementation                                           | Severe      | Unlikely   |
| R3.2    | Utilstrækkelig data access logging og auditing                | Implementér comprehensive logging; log alle data access events; test audit trail completeness; sikr GDPR compliance     | Significant | Possible   |
| R3.3    | Manglende data retention og deletion policies (GDPR)          | Implementér automated data lifecycle management; test deletion procedures; dokumentér compliance med juridisk ekspert   | Significant | Possible   |
| R3.4    | Sensitive data logges i plain text eller error messages       | Inkluder logging audits i code reviews; implementér log sanitization; generic error messages til brugere; mask PII data | Significant | Possible   |
| R3.5    | Manglende consent management eller data processing agreements | Dokumentér data processing; implementér consent tracking; validér med Compliance Officer; test consent flows            | Significant | Unlikely   |

### Team & Ressource (Parent: R4)

| Risk ID | Risk                                                                                  | Mitigation                                                                                                                                 | Severity    | Likelihood |
| :------ | :------------------------------------------------------------------------------------ | :----------------------------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R4.1    | Sygdom eller uventet fravær blandt teammedlemmer                                      | Implementér cross-training og backup-roller; brug fleksible ressourcer fra konsulenthus' pulje; dokumentér procedurer grundigt             | Moderate    | Possible   |
| R4.2    | Pludselig afgang af nøglemedlemmer eller rolleansvarlige                              | Udfør regelmæssig succession planning og vidensoverførselssessioner; integrér peer reviews for at sprede viden; dokumentér kritisk viden   | Significant | Unlikely   |
| R4.3    | Kompetencemangel i specifikke teknologier eller testmetoder                           | Tilbyd løbende træning og certificeringer; brug eksterne konsulenter ved behov; implementér knowledge sharing sessions og pair programming | Moderate    | Possible   |
| R4.4    | Kommunikationsbarrierer mellem team eller med kunder (sprogbarrierer, misforståelser) | Etablér klare kommunikationskanaler; brug regelmæssige stand-ups og reviews; dokumentér beslutninger; sæt tid til side for forståelse      | Moderate    | Possible   |
| R4.5    | Overbelastning af team på grund af flere samtidige projekter                          | Prioritér kritiske opgaver; omfordel story points løbende; projektleder tildeler ekstra ressourcer ved behov; eskalér tidligt              | Minor       | Possible   |
| R4.6    | Motivations- og samarbejdsproblemer i teamet                                          | Hold løbende teambuilding events; skab godt arbejdsmiljø; addresser konflikter tidligt; regelmæssige one-on-ones                           | Moderate    | Possible   |
| R4.7    | Manglende overholdelse af roller og ansvar                                            | Integrér CI/CD-checks for at blokere commits uden tests; udfør audits for compliance; inkluder i Definition of Done                        | Significant | Unlikely   |

### Udvikling & Kvalitet (Parent: R5)

| Risk ID | Risk                                                 | Mitigation                                                                                            | Severity    | Likelihood |
| :------ | :--------------------------------------------------- | :---------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R5.1    | Code coverage under threshold i kritiske komponenter | Sæt minimum coverage targets; block CI builds ved utilstrækkelig coverage; prioritér kritiske moduler | Significant | Possible   |
| R5.2    | Manglende tests af edge cases og boundary conditions | Brug boundary value analysis; test min/max values; reviewers skal validere edge case coverage         | Moderate    | Possible   |
| R5.3    | Concurrency issues ikke opdaget af tests             | Test race conditions og multi-threading scenarios; stress tests; test concurrent modifications        | Significant | Possible   |

### Infrastruktur & Miljø (Parent: R6)

| Risk ID | Risk                                                            | Mitigation                                                                                                                                | Severity    | Likelihood |
| :------ | :-------------------------------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R6.1    | Ustabilitet i testmiljøer på grund af konfigurationsfejl        | Automatisér miljøopsætning via DevOps pipelines; udfør miljøaudits månedligt; version control for configs                                 | Moderate    | Possible   |
| R6.2    | Miljøafvigelser mellem test og produktion (configuration drift) | Sikr produktionslignende miljøer; brug containers for konsistens; dokumentér alle miljøforskelle; test med samme konfiguration            | Significant | Possible   |
| R6.3    | Forskellige software versioner i test vs. produktion            | Synkroniser versioner via CI/CD; automatiser version checks; test i staging miljø før produktion; brug version pinning                    | Significant | Possible   |
| R6.4    | Test data kvalitet matcher ikke produktionsdata                 | Brug anonymiserede produktionsdata; generer realistiske test datasets; test med edge cases og realistic volumes                           | Moderate    | Possible   |
| R6.5    | Netværksproblemer eller connectivity issues                     | Overvåg netværk via monitoring tools; hav fallback til offline tests hvor muligt; dokumentér network dependencies; test network failures  | Moderate    | Possible   |
| R6.6    | Strømudfald eller hardwarefejl i testmiljøer                    | Implementér redundante setups med backup-generatorer; test failover procedurer periodisk; brug cloud backup; dokumentér disaster recovery | Significant | Unlikely   |

### Infrastruktur & Miljø (Parent: R7)

| Risk ID | Risk                                                            | Mitigation                                                                                                           | Severity    | Likelihood |
| :------ | :-------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R7.1    | Tredjeparts API'er eller services oplever downtime under tests  | Brug mocks og stubs i unit/integration tests; implementér fallback mekanismer; test degraded scenarios; monitor SLA  | Significant | Possible   |
| R7.2    | API contract ændringer fra tredjepart uden varsel               | Overvåg API versioner; subscribe til change notifications                                                            | Significant | Possible   |
| R7.3    | Database eller message queue downtime                           | Brug clustered setups; test multi-region failovers; implementér retry logic; automatiser backups                     | Significant | Possible   |
| R7.4    | Forsinkelser fra leverandører (tools, hardware, cloud services) | Diversificér leverandører; inkluder buffertider i tidsplaner; hav kontraktlige SLA'er; plan alternative solutions    | Moderate    | Possible   |
| R7.5    | Deprecation af eksterne værktøjer eller API'er                  | Udvikl in-house eller find alternativer; prioritér Microsoft eller vettede kilder; overvåg end-of-life announcements | Significant | Unlikely   |

### Integration & Deployment (Parent: R8)

| Risk ID | Risk                                                       | Mitigation                                                                                                                          | Severity    | Likelihood |
| :------ | :--------------------------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R8.1    | Deployment crasher på grund af inkompatible dependencies   | Test deployments i staging miljø; inkluder smoke tests post-deploy; version lock dependencies                                       | Significant | Possible   |
| R8.2    | Configuration fejl under deployment                        | Brug configuration management tools; validér config før deployment; test configuration changes i staging; use environment variables | Moderate    | Possible   |
| R8.3    | Database migration fejl forårsager data corruption         | Test migrations i staging først; implementér rollback scripts; backup før migration; validér data integrity post-migration          | Significant | Possible   |
| R8.4    | Rollback failures efterlader systemet i inconsistent state | Test rollback procedures regelmæssigt; dokumentér rollback steps; automatiser rollback hvor muligt; maintain previous version ready | Significant | Possible   |
| R8.5    | Manglende monitoring efter deployment opdager ikke fejl    | Implementér comprehensive monitoring og alerting; inkluder health checks; overvåg key metrics post-deploy; alert on anomalies       | Moderate    | Possible   |

### Integration & Deployment (Parent: R9)

| Risk ID | Risk                                                                 | Mitigation                                                                                            | Severity    | Likelihood |
| :------ | :------------------------------------------------------------------- | :---------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R9.1    | Regression bugs efter refactoring bryder eksisterende funktionalitet | Kør fuld regression suite i CI/CD efter hver refactoring; comprehensive test coverage før refactoring | Significant | Possible   |
| R9.2    | Performance degradering efter code changes                           | Kør performance tests før og efter deployment; overvåg key metrics                                    | Moderate    | Possible   |

### Udvikling & Kvalitet (Parent: R10)

| Risk ID | Risk                                                        | Mitigation                                                                                                                     | Severity    | Likelihood |
| :------ | :---------------------------------------------------------- | :----------------------------------------------------------------------------------------------------------------------------- | :---------- | :--------- |
| R10.1   | User stories mangler klare acceptance criteria              | Involvér Product Owner i acceptance criteria definition; dokumentér krav grundigt                                              | Significant | Possible   |
| R10.2   | Ændrede krav opdages sent i udviklingscyklus                | Opdater tests ved requirement changes; kommunikér ændringer til alle stakeholders; sprint reviews                              | Significant | Possible   |
| R10.3   | Testplaner ikke opdateret efter kravændringer               | Dokumentér ændringer i testplaner; sync testplaner med requirements; version control for test documents                        | Moderate    | Possible   |
| R10.4   | Kompleks business logic komplicerer testing                 | Modulært design; brug dependency injection; refactor kompleks logic løbende; isolér business rules i dedikerede komponenter    | Moderate    | Possible   |
| R10.5   | Misforståelse af forretningsdomæne fører til forkerte tests | Involvér domain experts i test design; udfør knowledge sharing sessions; dokumentér forretningslogik; brug ubiquitous language | Moderate    | Possible   |
