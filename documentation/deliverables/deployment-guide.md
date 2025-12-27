# Deployment Guide for MToGo

Denne guide beskriver, hvordan MToGo-systemet kan køres og deployes:

- Lokalt med Docker Compose (hurtigst for demo/dev)
- Lokalt til Kubernetes med Terraform (matcher cloud-setup tættere)
- Til Azure med Terraform + GitHub Actions (AKS + PostgreSQL Flexible Server + Azure Managed Grafana)

## Indholdsfortegnelse

- [1. Valg af deployment-metode](#1-valg-af-deployment-metode)
- [2. Lokal: Docker Compose](#2-lokal-docker-compose)
- [3. Lokal: Kubernetes via Terraform](#3-lokal-kubernetes-via-terraform)
- [4. Azure: GitHub Actions + Terraform](#4-azure-github-actions--terraform)
- [5. Azure: Manuelle Terraform-kommandoer (valgfrit)](#5-azure-manuelle-terraform-kommandoer-valgfrit)
- [6. Verifikation og smoke tests](#6-verifikation-og-smoke-tests)
- [7. Fejlsøgning (typiske fejl)](#7-fejlsøgning-typiske-fejl)
- [8. Sikkerhed og drift-noter](#8-sikkerhed-og-drift-noter)

## 1. Valg af deployment-metode

### Anbefalinger

- **Til hurtig lokal demo/dev**: Docker Compose.
- **Til "cloud-lignende" lokal kørsel** (Ingress, K8s resources, monitoring i cluster): Terraform local.
- **Til cloud**: GitHub Actions workflows (Azure) + Terraform.

### Hvad kører hvor?

| Område                             | Docker Compose (lokal)         | Terraform local (lokal K8s)                                | Terraform Azure (AKS)                                               |
| ---------------------------------- | ------------------------------ | ---------------------------------------------------------- | ------------------------------------------------------------------- |
| App services                       | Ja                             | Ja                                                         | Ja                                                                  |
| PostgreSQL                         | Ja (container)                 | Ja (in-cluster)                                            | Ja (Azure PostgreSQL)                                               |
| Kafka                              | Ja (container)                 | Ja (in-cluster)                                            | Ja (in-cluster)                                                     |
| Monitoring (Prometheus/AM/Grafana) | Ja (containers)                | Ja (Helm i cluster)                                        | Metrics i Azure Monitor Workspace + Grafana i Azure Managed Grafana |
| Grafana logins                     | Brugernavn/password fra `.env` | Brugernavn/password fra `terraform/local/terraform.tfvars` | Entra ID + Azure RBAC                                               |

## 2. Lokal: Docker Compose

### 2.1 Forudsætninger

- Docker Desktop (Linux containers) eller Docker Engine
- Docker Compose v2 (`docker compose`)

### 2.2 Konfiguration (.env)

I repo-roden findes en eksempel-fil:

- `.env.example`

Opret en `.env` (repo-roden):

```bash
# macOS/Linux/WSL/Git Bash
cp .env.example .env
```

```powershell
# PowerShell
Copy-Item .env.example .env
```

Udfyld minimum disse:

- `MANAGEMENT_USERNAME` og `MANAGEMENT_PASSWORD` (bruges af ManagementService)
- `GRAFANA_USER` og `GRAFANA_PASSWORD` (bruges af begge lokale Grafana containers)
- `DISCORD_WEBHOOK_ALERT` (bruges af Alertmanager til Discord-notifikationer)

Bemærk:

- Hvis `DISCORD_WEBHOOK_ALERT` ikke er sat korrekt, starter Alertmanager stadig, men vil logge fejl ved afsendelse.

### 2.3 Start

Start alt (byg og kør):

```bash
docker compose up -d --build
```

Stop (bevarer volumes):

```bash
docker compose down
```

Stop + fjern volumes (nulstiller databaser m.m.):

```bash
docker compose down -v
```

### 2.4 Endpoints (Docker Compose)

Disse er direkte angivet i `docker-compose.yml`:

| Service       | URL                   |
| ------------- | --------------------- |
| Website       | http://localhost:8081 |
| API Gateway   | http://localhost:8080 |
| Legacy API    | http://localhost:8082 |
| Prometheus    | http://localhost:9090 |
| Grafana (KPI) | http://localhost:3000 |
| Grafana (SLO) | http://localhost:3001 |
| Alertmanager  | http://localhost:9093 |

## 3. Lokal: Kubernetes via Terraform

Denne metode deployer til din nuværende lokale Kubernetes-context (Docker Desktop Kubernetes eller Minikube) via `terraform/local`.

### 3.1 Forudsætninger

- Docker Desktop med Kubernetes aktiveret (eller Minikube)
- `kubectl`
- `terraform`
- PowerShell (Windows PowerShell 5.1 eller PowerShell 7+)

### 3.2 Konfiguration (terraform.tfvars)

Kopiér eksempel:

- `terraform/local/terraform.tfvars.example` -> `terraform/local/terraform.tfvars`

Vigtige felter:

- `image_registry` og `image_tag` (matcher images bygget af `deploy.ps1 -Build`)
- `postgres_admin_password` (til in-cluster postgres)
- `install_ingress` (default: `true`)
- `install_monitoring` (default: `true`)
- `discord_webhook_url` (valgfri; Discord-alerts fra Alertmanager)
- Grafana admin credentials til de to Grafana instanser:
  - `grafana_kpi_admin_username` / `grafana_kpi_admin_password`
  - `grafana_slo_admin_username` / `grafana_slo_admin_password`

### 3.3 Deploy (anbefalet via deploy.ps1)

Kør fra repo-roden:

```powershell
.\terraform\deploy.ps1 -Context local -Build
```

Hvad `-Build` gør:

- Scriptet bygger Docker images lokalt og tagger dem som `mtogo/<image>:latest`.
  - Eksempel: `mtogo/mtogo-gateway:latest`, `mtogo/mtogo-website:latest`, `mtogo/mtogo-legacy:latest`.
  - `image_registry` i `terraform/local/terraform.tfvars` skal matche registry-delen (default: `mtogo`).
- Det bygger gateway, website, services og legacy.

Deploy uden build (forudsætter images allerede findes lokalt):

```powershell
.\terraform\deploy.ps1 -Context local
```

Destroy:

```powershell
.\terraform\deploy.ps1 -Context local -Destroy
```

### 3.4 Sikkerhedscheck i scriptet (vigtigt)

`terraform/deploy.ps1` har et vigtigt sikkerhedscheck:

- Ved `-Context local` stopper scriptet, hvis din nuværende `kubectl` context "ligner AKS" (matcher `aks`).
- Hvis `docker-desktop` context findes, forsøger scriptet at skifte til den automatisk.

### 3.5 Endpoints (Terraform local)

Efter succesfuld deploy printer scriptet Terraform outputs (feltet `endpoints`):

- Website (Ingress): `https://localhost/`
- API (Ingress -> Gateway): `https://localhost/api/v1/`
  - Ingress router alt under `/api*` til gateway, og gateway's versionerede endpoints ligger under `/api/v1/...`.
- Legacy API (Ingress): `https://localhost/legacy`

Bemærk (vigtigt):

- Terraform local bruger HTTPS med et self-signed certifikat. Browseren vil typisk vise en certifikat-advarsel første gang.

Monitoring (installeres i cluster og eksponeres som LoadBalancer på Docker Desktop):

- Grafana (KPI): `http://localhost:3000`
- Grafana (SLO): `http://localhost:3001`
- Prometheus: `http://localhost:9090`
- Alertmanager: `http://localhost:9093`

### 3.6 Monitoring og alerting (Terraform local)

Når `install_monitoring=true`:

- Prometheus+Alertmanager installeres via Helm (prometheus-community/prometheus)
- Alertmanager konfigureres via template `monitoring/alertmanager/alertmanager.yml.template`
- KPI alert rules kommer fra `monitoring/prometheus/alert_rules.yml`
- Scriptet forsøger at importere KPI alert rules ind i **Grafana-managed alerting** i KPI Grafana, så man kan se dem under Grafana -> Alerting.

## 4. Azure: GitHub Actions + Terraform

Azure-deployment er designet til at køres via GitHub Actions workflows.

### 4.1 Overblik over Azure-ressourcer

Terraform i `terraform/azure` provisionerer:

- Resource Group: `rg-<project_name>-<environment>`
- AKS cluster: `aks-<project_name>-<environment>-<locationSlug>`
- Azure Database for PostgreSQL Flexible Server (Postgres 16) + databases pr. service
- Azure Monitor Workspace (Managed Prometheus storage/query endpoint)
- 2x Azure Managed Grafana:
  - SLO: `grafana-slo-<project>-<env>`
  - KPI: `grafana-kpi-<project>-<env>`

App'en deployes som Kubernetes resources via Terraform modulet `terraform/modules/mtogo-app`.

Vigtigt for Azure:

- PostgreSQL deployes **ikke** i AKS (Azure bruger managed Postgres).
- Kafka deployes **i** AKS (single-node / cluster-internal).
- Ingress-NGINX installeres i AKS.
  - Platformen er sat op med HTTPS og et self-signed cert (SAN inkluderer ingress public IP), så de primære URLs er typisk `https://<ingress-ip>/...`.
  - Da der ikke bruges DNS i denne opsætning, vil browseren ofte vise en TLS-advarsel (self-signed).

### 4.2 Forudsætninger (Azure)

- Azure subscription
- GitHub repo med Actions enabled
- Azure Service Principal + federated credentials til GitHub OIDC
- Azure Storage Account + container til Terraform remote state
- GHCR (GitHub Container Registry) images, som AKS kan trække

> Denne sektion viser også en **fuld, konkret kommando-liste** (PowerShell) til at oprette alt det ovenstående og udlede de værdier, der skal sættes som GitHub Secrets.

#### 4.2.1 Service Principal (OIDC) - hvad workflowet forventer

Workflows bruger `azure/login@v2` med OIDC:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Service principal skal mindst have **Contributor** på subscription eller på den resource group, hvor der deployes.

**Vigtigt (RBAC for Terraform):**

Terraform-konfigurationen i `terraform/azure` opretter også Azure RBAC role assignments (fx `azurerm_role_assignment.*`). Det kræver rettigheden `Microsoft.Authorization/roleAssignments/write`, som **Contributor ikke giver**.

Derfor skal service principal også have én af disse:

- **User Access Administrator** (anbefalet) på relevant scope, eller
- **Owner** på relevant scope.

For en enkel “det virker”-opsætning til eksamens/demo: giv **Contributor + User Access Administrator** på subscription-scope.

#### 4.2.2 Terraform remote state backend

Workflows genererer en `backend.tf` og kører `terraform init` med disse GitHub Secrets:

- `TFSTATE_RESOURCE_GROUP`
- `TFSTATE_STORAGE_ACCOUNT`
- `TFSTATE_CONTAINER`

State key er miljø-afhængigt:

- `mtogo.dev.tfstate`
- `mtogo.staging.tfstate`
- `mtogo.prod.tfstate`

#### 4.2.3 Kommandoer (PowerShell) til at oprette OIDC + tfstate + udlede GitHub Secrets

Kør nedenstående i **PowerShell** (Windows PowerShell 5.1 eller PowerShell 7+) fra en maskine med Azure CLI installeret.

> Forudsætning: du skal være logget ind i Azure CLI med en bruger, der kan oprette app/SP og tildele roller (typisk Owner eller User Access Administrator på subscription).

**1) Log ind og vælg subscription**

```powershell
az login

# (valgfrit) hvis du har flere subscriptions
az account list -o table
az account set --subscription "<SUBSCRIPTION_ID>"

# Print de værdier, du skal bruge som GitHub Secrets
az account show --query "{tenantId:tenantId, subscriptionId:id, user:user.name}" -o json
```

**2) Opret Entra App + Service Principal (OIDC)**

`AZURE_CLIENT_ID` er App (client) ID = `appId`.

```powershell
# Opret app
$app = az ad app create --display-name "mtogo-github-oidc" -o json | ConvertFrom-Json
$appId = $app.appId

# Opret service principal for app'en
az ad sp create --id $appId -o none

# Find Service Principal objectId (bruges til role assignments)
$spObjectId = az ad sp show --id $appId --query id -o tsv

Write-Host "AZURE_CLIENT_ID (appId) = $appId"
Write-Host "Service principal objectId = $spObjectId"
```

**3) Giv nødvendige Azure roller (RBAC)**

```powershell
$subId = (az account show --query id -o tsv)

# Terraform skal kunne provisionere ressourcer
az role assignment create \
  --assignee-object-id $spObjectId \
  --assignee-principal-type ServicePrincipal \
  --role "Contributor" \
  --scope "/subscriptions/$subId" -o none

# Terraform opretter også role assignments (azurerm_role_assignment.*)
az role assignment create \
  --assignee-object-id $spObjectId \
  --assignee-principal-type ServicePrincipal \
  --role "User Access Administrator" \
  --scope "/subscriptions/$subId" -o none
```

**4) Opret Terraform remote state backend (RG + Storage Account + Container)**

Storage account-navn skal være globalt unikt og kun indeholde små bogstaver/tal.

```powershell
$location = "northeurope"
$tfstateRg = "rg-mtogo-tfstate"
$tfstateContainer = "tfstate"

az group create -n $tfstateRg -l $location -o none

# Lav et unikt storage account navn
$suffix = ([Guid]::NewGuid().ToString('N').Substring(0,8))
$tfstateSa = ("mtogotfstate" + $suffix).ToLower()

az storage account create \
  -g $tfstateRg \
  -n $tfstateSa \
  -l $location \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 -o none

az storage container create \
  --account-name $tfstateSa \
  -n $tfstateContainer \
  --auth-mode login -o none

Write-Host "TFSTATE_RESOURCE_GROUP  = $tfstateRg"
Write-Host "TFSTATE_STORAGE_ACCOUNT = $tfstateSa"
Write-Host "TFSTATE_CONTAINER       = $tfstateContainer"
```

**5) Giv SP adgang til at læse/skrive Terraform state i storage account**

```powershell
$tfstateSaId = az storage account show -g $tfstateRg -n $tfstateSa --query id -o tsv

az role assignment create \
  --assignee-object-id $spObjectId \
  --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" \
  --scope $tfstateSaId -o none
```

**6) Opret federated credentials til GitHub OIDC (dev/staging/prod environments)**

Workflows kører med GitHub Environments `dev|staging|prod`. Derfor skal subjects matche:

`repo:<OWNER>/<REPO>:environment:<ENV>`

```powershell
$owner = "<GITHUB_OWNER>"          # fx "Peter537"
$repo  = "<GITHUB_REPO>"           # fx "Soft2-Eksamensopgave"

$issuer = "https://token.actions.githubusercontent.com"
$aud = "api://AzureADTokenExchange"

foreach ($e in @("dev","staging","prod")) {
  $name = "github-oidc-environment-$e"
  $subject = "repo:$owner/${repo}:environment:$e"

  $payload = @{
    name = $name
    issuer = $issuer
    subject = $subject
    audiences = @($aud)
    description = "GitHub Actions OIDC for $owner/${repo} environment $e"
  } | ConvertTo-Json -Depth 10

  $tmp = Join-Path $env:TEMP "$name.json"
  $payload | Out-File -FilePath $tmp -Encoding utf8
  az ad app federated-credential create --id $appId --parameters $tmp -o none
}

az ad app federated-credential list --id $appId --query "[].{name:name,subject:subject}" -o table
```

**7) Hvad skal jeg skrive i GitHub Secrets? (fra ovenstående output)**

- `AZURE_CLIENT_ID` = `$appId`
- `AZURE_TENANT_ID` = output fra `az account show` (`tenantId`)
- `AZURE_SUBSCRIPTION_ID` = output fra `az account show` (`subscriptionId`)
- `TFSTATE_RESOURCE_GROUP` = `$tfstateRg`
- `TFSTATE_STORAGE_ACCOUNT` = `$tfstateSa`
- `TFSTATE_CONTAINER` = `$tfstateContainer`

Resten af secrets (GHCR/Discord/Management/Postgres password) oprettes separat som beskrevet i afsnit 4.3.

### 4.3 Krævede GitHub Secrets

Opret følgende secrets i repoet (Settings -> Secrets and variables -> Actions):

**Azure login (OIDC)**

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

**Terraform state backend (Azure Storage)**

- `TFSTATE_RESOURCE_GROUP`
- `TFSTATE_STORAGE_ACCOUNT`
- `TFSTATE_CONTAINER`

**Postgres admin password**

- `POSTGRES_ADMIN_PASSWORD`

**GHCR pull credentials (AKS imagePullSecret)**

- `GHCR_USERNAME`
- `GHCR_TOKEN` (PAT med `read:packages`)

**ManagementService seeded admin**

- `MANAGEMENT_USERNAME`
- `MANAGEMENT_PASSWORD`

**KPI alerting**

- `DISCORD_WEBHOOK_URL`

Bemærk:

- Workflows sender mange af disse ind som `TF_VAR_*` miljøvariabler, så Terraform får værdierne uden at checke `terraform.tfvars` ind.

### 4.4 Workflow: Deploy til Azure (azure_deploy.yml)

Workflow: `.github/workflows/azure_deploy.yml`

Startes manuelt via GitHub UI (workflow_dispatch) med inputs:

- `environment`: `dev`, `staging`, `prod` (default: `prod`)
- `image_tag`: tom = `latest` (default i workflow), ellers valgfri tag
- `node_count`: default `1` (i workflow)
- `action`: `plan` eller `apply`

Bemærk om `node_count` (AKS):

- Terraform default har `enable_auto_scaling=true`, så `node_count` fungerer som initial/minimum node count (ikke nødvendigvis et "fast" antal noder).

Hvad der sker:

1. Workflow bestemmer image tag
2. Logger ind i Azure via OIDC
3. Terraform init mod Azure Storage state
4. Terraform plan og (hvis valgt) apply
5. Læser Terraform outputs (resource group, AKS name, website/api URLs osv.)
6. Provisionerer Azure Managed Grafana (dashboards + alerting)
7. Verificerer deploy via kubectl (pods/services og viser URLs)

Vigtigt om `image_tag`:

- AKS puller images fra `ghcr.io/<repoOwner>/<service>:<image_tag>`.
- Hvis images med det tag **ikke findes** i GHCR, vil pods få `ImagePullBackOff`.

### 4.5 Workflow: Destroy Azure (azure_destroy.yml)

Workflow: `.github/workflows/azure_destroy.yml`

Sikkerhed:

- Kræver at du skriver `DESTROY` i input `confirm`.

Workflow kører `terraform destroy` mod det valgte environment/state key.

### 4.6 Workflow: Staging deploy + test + cleanup (azure_staging_test.yml)

Workflow: `.github/workflows/azure_staging_test.yml`

Flow:

1. Deploy staging med `TF_VAR_environment=staging` og `image_tag=latest`
2. Venter på at alle deployments er klar i namespace `mtogo`
3. Port-forwarder gateway til localhost:8080
4. Kører tests:

   - Integration tests
   - E2E tests
   - Performance tests (workflown sætter `RUN_PERFORMANCE_TESTS=true` og bruger test filter)

5. Kører altid cleanup: `terraform destroy` for staging

## 5. Azure: Manuelle Terraform-kommandoer (valgfrit)

I praksis bør der bruges GitHub Actions til production, men til lokal afprøvning af Azure-flowet kan du køre det via det samme wrapper-script som til local fra repo-roden:

```powershell
.\terraform\deploy.ps1 -Context azure
```

Destroy (slet Azure miljøet igen) fra repo-roden:

```powershell
.\terraform\deploy.ps1 -Context azure -Destroy
```

Bemærk:

- `deploy.ps1` kører `terraform init/apply/destroy` i `terraform/azure` og forsøger at håndtere typiske Azure-fejl (fx provider-registrering og "resource already exists" -> import).
- Azure deploy bygger **ikke** lokale Docker images. AKS trækker images fra **GHCR** (`ghcr.io/<repoOwner>/<service>:<tag>`). Hvis image-tagget ikke findes i GHCR, får pods typisk `ImagePullBackOff`.
- Hvis du kører uden GitHub Actions, skal du selv sikre:
  - Terraform state backend (Azure Storage) er sat op, hvis du vil have samme remote state som workflows.
  - At Grafana provisioning bliver kørt:
    - Workflows kører `terraform/azure/provision_grafana.ps1` efter apply.
    - `deploy.ps1`-flowet på Azure synkroniserer også dashboards/alerting baseret på Terraform outputs, men kræver at `discord_webhook_url` er sat (til KPI alerting).

## 6. Verifikation og smoke tests

### 6.1 Docker Compose

- `docker compose ps` -> alle services "Up"
- Website loader: http://localhost:8081
- Gateway health: prøv `http://localhost:8080/health`
- Grafana login: http://localhost:3000 (brug `.env` credentials)
- Prometheus: http://localhost:9090 -> Status -> Targets

### 6.2 Terraform local

- `kubectl get pods -n mtogo`
- `kubectl get svc -n mtogo`
- `kubectl get ingress -n mtogo`
- Website: https://localhost/

Bemærk:

- Ingress er konfigureret til at redirecte HTTP -> HTTPS, og TLS er self-signed.

### 6.3 Azure

- GitHub Actions run: tjek at `Terraform Apply` og `Verify deployment` er grøn
- `az aks get-credentials ...` (se Terraform output `connect_instructions`)
- `kubectl get pods -n mtogo -o wide`
- Find ingress IP:

```bash
kubectl get svc -n ingress-nginx ingress-nginx-controller
```

- Website: `https://<ingress-ip>/`
- API (Ingress -> Gateway): `https://<ingress-ip>/api/v1`
  - Ingress router alt under `/api*` til gateway, og gateway’s versionerede endpoints ligger under `/api/v1/...`.
- Legacy: `https://<ingress-ip>/legacy`

Grafana (Azure Managed Grafana):

- KPI endpoint: Terraform output `kpi_grafana_endpoint`
- SLO endpoint: Terraform output `slo_grafana_endpoint`

Log ind med Entra ID; adgang styres via Azure RBAC (Grafana Viewer/Editor/Admin).

## 7. Fejlsøgning (typiske fejl)

### 7.1 Docker Compose: manglende env vars

Symptomer:

- Grafana login virker ikke
- ManagementService fejler ved startup
- Alertmanager kan ikke sende til Discord

Løsning:

- Verificér `.env` eksisterer og har værdier
- Kør `docker compose logs -f <service>` for at se præcis fejl

### 7.2 Terraform local: forkert Kubernetes context

Symptom:

- Scriptet fejler med "Current kubectl context looks like AKS"

Løsning:

- Skift til `docker-desktop` eller anden lokal context:

```bash
kubectl config get-contexts
kubectl config use-context docker-desktop
```

### 7.3 Terraform init state lock

Symptom:

- `terraform init` fejler pga. lock

Løsning:

- Luk andre Terraform-processer/terminals og kør igen (scriptet forsøger automatisk flere gange)

### 7.4 Azure: provider registration errors

Symptom:

- Terraform/azurerm fejl som "MissingSubscriptionRegistration"

Løsning:

- `terraform/deploy.ps1` forsøger at registrere nødvendige providers:
  - `Microsoft.ContainerService`, `Microsoft.Monitor`, `Microsoft.Dashboard`, `Microsoft.Network`, `Microsoft.DBforPostgreSQL`, `Microsoft.OperationalInsights`

Hvis du kører manuelt, kan du registrere i Azure CLI:

```bash
az provider register -n Microsoft.ContainerService
```

### 7.5 Azure: ImagePullBackOff

Symptom:

- Pods kan ikke trække images

Check:

- At `GHCR_USERNAME`/`GHCR_TOKEN` secrets er korrekte
- At images med `image_tag` findes i GHCR
- `kubectl describe pod <pod> -n mtogo`

### 7.6 Azure: Grafana dashboards mangler

I Azure provisioneres dashboards af `terraform/azure/provision_grafana.ps1` (kørt i workflows).

Hvis dashboards mangler:

- Tjek workflow step "Provision Azure Managed Grafana"
- Tjek at Grafana MSI har "Monitoring Data Reader" på Azure Monitor Workspace (Terraform opretter role assignments)
