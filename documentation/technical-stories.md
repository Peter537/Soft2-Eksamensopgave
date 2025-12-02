## Technical Stories

Dette dokument indeholder technical stories (TS) for implementering af forskellige funktioner og forbedringer. Hver historie inkluderer størrelse, prioritet, beskrivelse og acceptkriterier (AC).

---

**Size:** L  
**Priority:** High  
**TS-1:** Implement JWT Authentication  
**As a** Security Expert  
**I want** to implement JWT authentication across all microservices so that unauthorized access to sensitive endpoints is prevented, ensuring compliance with the security plan.  
**AC-1:** Configure JWT middleware using a Shared service.  
**AC-2:** Validate tokens on protected routes.  
**AC-3:** Handle token expiration.  
**AC-4:** Implement RBAC based on JWT claims.

---

**Size:** M  
**Priority:** Medium  
**TS-2:** Configure CORS Policies  
**As a** Security Expert  
**I want** to configure CORS policies for API gateways and frontend so that cross-origin requests from the website service are allowed only from trusted domains, reducing exposure to CSRF attacks.  
**AC-1:** Set CORS headers (e.g., Access-Control-Allow-Origin) to whitelist frontend URLs.  
**AC-2:** Apply to WebSocket endpoints for real-time notifications.  
**AC-3:** Log and block non-compliant origins.

---

**Size:** L  
**Priority:** High  
**TS-3:** Set Up Prometheus and Grafana  
**As a** DevOps  
**I want** to set up Prometheus for metrics collection and Grafana for visualization so that system health can be monitored in real-time, supporting SLO adherence.  
**AC-1:** Deploy Prometheus in Kubernetes.  
**AC-2:** Expose /metrics endpoints in all services.  
**AC-3:** Configure Grafana dashboards for key SLOs.  
**AC-4:** Integrate with CI/CD for auto-deployment.

---

**Size:** M  
**Priority:** High  
**TS-4:** Configure Alerting  
**As a** DevOps  
**I want** to configure notifications in Grafana/Prometheus so that alerts are triggered on SLO violations.  
**AC-1:** Define alert rules for KPIs.  
**AC-2:** Set up contact points and routing in Prometheus Alertmanager.  
**AC-3:** Ensure notifications include context.

---

**Size:** M  
**Priority:** High  
**TS-5:** Define Prometheus Metrics  
**As a** DevOps  
**I want** to define Prometheus metrics aligned with SLOs and KPIs so that custom counters, gauges, etc. track business goals.  
**AC-1:** Create metrics for SLOs and KPIs.  
**AC-2:** Instrument services with Prometheus exporters.  
**AC-3:** Create JSON-dashboards for Grafana.

---

**Size:** L  
**Priority:** Medium  
**TS-6:** Set Up Performance Tests with k6  
**As a** DevOps  
**I want** to set up performance tests using k6 so that load scenarios validate SLOs under stress.  
**AC-1:** Write k6 scripts for key user flows.  
**AC-2:** Integrate into workflow for automated runs.  
**AC-3:** Validate correct performance-tool in documentation.

---

**Size:** M  
**Priority:** Low  
**TS-8:** Set Up SonarQube for Static Code Analysis  
**As an** Udvikler  
**I want** to set up SonarQube for static code analysis so that code quality gates are enforced before merges, reducing technical debt in services.  
**AC-1:** Configure SonarQube server and scanner.  
**AC-2:** Run scans on PRs.  
**AC-3:** Block merges on quality gate failures.

---

**Size:** L  
**Priority:** Medium  
**TS-9:** Set Up Terraform for Infrastructure Provisioning  
**As a** DevOps  
**I want** to set up Terraform for infrastructure provisioning so that repeatable IaC definitions resources ensure consistent environments.  
**AC-1:** Write Terraform modules for databases, networks, and services.  
**AC-2:** Integrate with GitHub Actions for plan/apply on merges.  
**AC-3:** Documentation state definitions.

---

**Size:** M  
**Priority:** Medium  
**TS-11:** Implement Shared Testing Frameworks  
**As an** Udvikler  
**I want** to implement shared testing frameworks so that common utilities are reusable across service test plans, reducing duplication in integration tests.  
**AC-1:** Create a MToGo.Testing package for test helpers.

---
