# Non-Functional Requirements (NFR)

Dette dokument beskriver de ikke-funktionelle krav til MToGo food delivery platformen.

## Indholdsfortegnelse

- [Performance](#performance)
- [Availability](#availability)
- [Scalability](#scalability)
- [Security](#security)
- [Maintainability](#maintainability)
- [Usability](#usability)

---

## Performance

| Krav ID     | Beskrivelse                | Mål                                   |
| ----------- | -------------------------- | ------------------------------------- |
| **NFR-P01** | Order Creation Latency     | < 400ms (p95)                         |
| **NFR-P02** | API Response Time          | < 500ms for alle REST endpoints (p95) |
| **NFR-P03** | WebSocket Message Delivery | < 2 sekunder for notifikationer       |
| **NFR-P04** | Database Query Response    | < 100ms for standard queries          |

---

## Availability

| Krav ID     | Beskrivelse                 | Mål                       |
| ----------- | --------------------------- | ------------------------- |
| **NFR-A01** | System Uptime               | >= 99.5% (30-day rolling) |
| **NFR-A02** | Order Service Availability  | >= 99.9%                  |
| **NFR-A03** | WebSocket Connection Uptime | >= 99.5%                  |
| **NFR-A04** | Kafka Event Publishing      | >= 99.95% success rate    |

---

## Scalability

| Krav ID     | Beskrivelse               | Mål                                                            |
| ----------- | ------------------------- | -------------------------------------------------------------- |
| **NFR-S01** | Concurrent Order Requests | >= 1500 requests/min                                           |
| **NFR-S02** | Horizontal Scaling        | Services skal kunne skaleres horisontalt via Docker/Kubernetes |
| **NFR-S03** | Database Connections      | Connection pooling med max 100 connections per service         |
| **NFR-S04** | Message Queue Throughput  | Kafka skal håndtere >= 5000 events/min                         |

---

## Security

| Krav ID       | Beskrivelse       | Mål                                                                       |
| ------------- | ----------------- | ------------------------------------------------------------------------- |
| **NFR-SEC01** | Authentication    | JWT-baseret authentication for alle API requests                          |
| **NFR-SEC02** | Authorization     | Role-based access control (RBAC) for Customer, Partner, Agent, Management |
| **NFR-SEC03** | Data Encryption   | HTTPS/TLS 1.3 for alle eksterne forbindelser                              |
| **NFR-SEC04** | Password Security | Bcrypt hashing med minimum cost factor 10                                 |
| **NFR-SEC05** | Input Validation  | Parameterized queries og input sanitization for alle endpoints            |
| **NFR-SEC06** | OWASP Compliance  | Overholdelse af OWASP Top 10 security guidelines                          |

---

## Maintainability

| Krav ID     | Beskrivelse   | Mål                                              |
| ----------- | ------------- | ------------------------------------------------ |
| **NFR-M01** | Code Coverage | >= 80% unit test coverage for kritiske services  |
| **NFR-M02** | Documentation | API dokumentation via OpenAPI/Swagger            |
| **NFR-M03** | Logging       | Struktureret logging med correlation IDs         |
| **NFR-M04** | Monitoring    | Prometheus metrics og Grafana dashboards         |
| **NFR-M05** | CI/CD         | Automatiseret build, test og deployment pipeline |

---

## Usability

| Krav ID     | Beskrivelse           | Mål                                                        |
| ----------- | --------------------- | ---------------------------------------------------------- |
| **NFR-U01** | Response Feedback     | Brugere skal have feedback inden 2 sekunder efter handling |
| **NFR-U02** | Error Messages        | Klare og handlingsrettede fejlmeddelelser                  |
| **NFR-U03** | Mobile Responsiveness | Website skal være responsivt på mobile enheder             |
| **NFR-U04** | Accessibility         | WCAG 2.1 Level AA compliance                               |

---

## Traceability

Disse ikke-funktionelle krav er relateret til følgende dokumenter:

- [Service Level Objectives](../business/service-level-objectives.md) - Detaljerede SLO targets
- [Security Plan](../workflows/security-plan.md) - Implementering af sikkerhedskrav
- [Test Strategi](../../internal-team/test-strategi.md) - Kvalitetssikring af krav
