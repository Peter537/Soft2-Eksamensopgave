# **Security plan for MToGo**

Version: 1.0  
Dato: 22. november 2025  
Forfattere: Oskar, Peter og Yusuf

## Indholdsfortegnelse

- [1. Indledning](#1-indledning)
- [2. Infrastruktur og Sikkerhed](#2-infrastruktur-og-sikkerhed)
- [3. Top Risici](#3-top-risici)
- [4. Key Security Controls](#4-key-security-controls)
- [5. Sikkerhedsgates/flow i pipeline](#5-sikkerhedsgatesflow-i-pipeline)
- [6. Best Practices for Docker og Kubernetes](#6-best-practices-for-docker-og-kubernetes)
- [7. Definition of Security-Done](#7-definition-of-security-done)

## 1. Indledning

Dette dokument fastlægger sikkerhedsplanen for MToGo for at sikre høj kvalitet samt en sikker oplevelse i udvikling, drift og brug af det samlede system.

## 2. Infrastruktur og Sikkerhed

- **Runtime:** C#/.NET 8 -> Docker -> Kubernetes -> Terraform
- **Ingress:** TLS + HSTS (all traffic HTTPS, HSTS enforced)
- **Secrets (CI):** GitHub Secrets
- **Secrets (local):** environment variables
- **Role-Based Access Control (RBAC):** Controls who can read/write secrets and information

## 3. Top Risici

| Risiko                | Beskrivelse                                                        | Løsning                                                                             |
| --------------------- | ------------------------------------------------------------------ | ----------------------------------------------------------------------------------- |
| Broken Access Control | F.eks. en bruger kan gætte et fremmed ID og læse/ændre andres data | Tjek altid ownership server‑side.                                                   |
| Vulnerable Components | Sårbare libraries/containers                                       | Dependabot/Trivy checks.                                                            |
| Secrets‑leak          | F.eks. keys i kode eller logs                                      | Brug Secrets, Gitleaks, og hold ekstra code review når secrets skal bruges i koden. |
| Denial of Service     | For mange requests gør appen langsom                               | Rate limiting + ressource begrænsninger.                                            |
| Injection Attacks     | F.eks. SQL/NoSQL injection                                         | Input validering + brug af ORM.                                                     |

## 4. Key Security Controls

- **Identity & Access:** JWT bearer, Authorization‑policies, ownership‑checks.
- **Input/Output:** Validation, strict JSON, security‑headers.
- **API Abuse:** Global rate limiting (per user/IP).
- **Secrets:** Ingen i kode/Docker‑lag; brug Secrets og local environment variables.
- **Supply chain:** Dependabot, CodeQL, Gitleaks (secrets), Trivy (file scan/image).
- **Password hashing**: BCrypt med work factor 12.
- **Defensive programming:**
  - Fail-fast.
  - Guard-clauses og konsekvent input/output-validering på alle grænseflader.
- **CORS**:
  - Strikt allow-list af frontend/gateway-origins (scheme+host+port), aldrig \*.
  - Begræns metoder/headers.
- Preflight‑TTL 1 time.
- Ingen AllowCredentials med \*.
- Config styres gennem environment variable: ALLOWED_CORS_ORIGINS.

## 5. Sikkerhedsgates/flow i pipeline

Før PR merge til main:

1. **CodeQL**: ingen HIGH/CRITICAL sårbarheder.
2. **Trivy**: ingen HIGH/CRITICAL i file scan/image.
3. **Gitleaks**: 0 findings.

**Branch protection**: Kræver checks. Releases/advanced ting er ikke påkrævet.

## 6. Best Practices

**Docker**:

- .dockerignore
- Multi-stage build (lille image)
- Kør som non-root og eksponér nødvendige porte
- Ingen secrets i image-lag

**Kubernetes**:

- **Deployment**: Service og minimum 2 replicas
- Ingress med HTTPS
- **Probes**: /health og /ready
- **securityContext**: runAsNonRoot, allowPrivilegeEscalation=false, readOnlyRootFilesystem=true
- **Resources**: CPU/memory requests/limits

## 7. Definition of Security-Done

- **CI grøn**: CodeQL, Trivy & Gitleaks.
- API dækker auth‑negatives, validation‑bounds, rate limiting, security‑headers.
- Containers kører som **non-root** med **probes** og **ressource-grænser**, hvor alt trafik går via **HTTPS** via Ingress.
- Passwords via **bcrypt**.
