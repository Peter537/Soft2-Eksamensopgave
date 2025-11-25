# MToGo Pipelines, Workflows & Automations

Dette dokument beskriver alle pipelines, der anvendes i udviklingen af MToGo-systemet som helhed, som alle er oprettet og eksekveret via GitHub Actions, hvilket sikrer ensartede builds, hurtig feedback og automatiske quality gates på tværs af alle services.

## Main/shared CI/CD-pipelines

- **cicd.yml**:  
  Main CI-workflow for alle services i repo'et. Kører på push og pull request til main og bruger et fælles detect_changes-job baseret på [dorny/paths-filter](https://github.com/dorny/paths-filter) til at finde ud af hvilke services der faktisk er ændret.
  For hver service findes der et separat job (customer_ci, order_ci, osv.), som:

  - kun kører når enten service-mapperne eller fælles kode er ændret
  - kører dotnet restore, dotnet build og dotnet test for service-specifikke projekter
  - bygger og uploader et Docker Image for den enkelte service til GitHub Container Registry (GHCR).

  Resultat: Vi undgår at bygge og teste hele løsningen ved hver merge til main; kun de services, der er berørt af ændringer, bliver bygget og testet.

- **security.yml**:  
  Security & Quality Gates workflow med fokus på sikkerhed, og ikke builds/tests. Kører på push og pull request til main og er ligeledes per service baseret via det samme detect_changes-job som i cicd.yml.

  Workflowet består af:

  - Et globalt CodeQL-job, som analyserer C#-koden, men kun kører, når der er ændringer i én eller flere services eller i fælles kode.
  - Et sæt per service security-jobs (security_customer, security_order, osv.), som kun kører for de services, der er ændret:

    - Gitleaks-scan af den pågældende service-mappe (secrets)
    - Trivy File System-scan af samme mappe (dependency vulnerabilities, HIGH/CRITICAL fejler workflowet)
    - Trivy-resultater uploades automatisk til GitHub Security-tabben.
    - Trivy Image-scan af det Docker Image, der blev bygget i cicd.yml (HIGH/CRITICAL fejler workflowet)

  Via branch protection på main er det konfigureret, at både CI og Security & Quality Gates workflows skal være grønne, før der kan merges til main. Dvs. man kan ikke få kode ind på main, hvis enten tests eller security-fejl ikke er rettet.

## Main/shared Workflows & Automations

- **weekly_codeql.yml**:  
  Ekstra CodeQL-scan, som kører ugentligt på main for at fange eventuelle nye sårbarheder i eksisterende kode.

- **performance.yml**:  
  Long-run performance tests af hele applikationen.  
  Denne pipeline kører efter et fastlagt schedule eller kan igangsættes manuelt.

- **dora_metrics.yml**:  
  Scheduled DORA metrics workflow, der indsamler og opdaterer DORA-målinger for repositoriet.

- **dependabot.yml**:  
  Konfiguration af GitHub Dependabot. Scheduled auto-scan af dependencies; opretter automatisk PR for opdateringer og flagger sikkerhedsrisici.

- **terraform.yml**:  
  Workflow til opsætning og opdatering af IaC. Bruges til at oprette/opdatere infrastrukturen (Kubernetes, databaser, osv.) på en kontrolleret måde.
