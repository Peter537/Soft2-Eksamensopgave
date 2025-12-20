# Terraform (Azure)

Terraform-konfiguration til at deploye MToGo-platformen til Azure.

Denne context provisionerer:

- **AKS** (Azure Kubernetes Service)
- **Azure Database for PostgreSQL Flexible Server** (+ databases pr. service)
- **Kubernetes-resources** via modulet [modules/mtogo-app](../modules/mtogo-app/)
- **Azure Monitor Workspace** (til metrics/Managed Prometheus)
- **Azure Managed Grafana (KPI)** (separat instans til KPI dashboards)
- **Azure Managed Grafana (SLO)** (separat instans til SLO dashboards)

## Hurtig start

**Production (anbefalet):** Brug GitHub Actions workflows:

- `.github/workflows/azure_deploy.yml`
- `.github/workflows/azure_destroy.yml`

De kører Terraform og provisionerer Azure Managed Grafana (dashboards + alerting) via `terraform/azure/provision_grafana.ps1`.

**Lokal test:** `terraform/deploy.ps1` kan bruges til lokal afprøvning, men den er ikke tænkt som “production deploy”.

## Forudsætninger

- Terraform installeret (`terraform` på PATH)
- Azure subscription
- Azure autentifikation (fx via Azure CLI):

```powershell
az login
```

Hvis du har flere subscriptions, så vælg den rigtige (ellers kan Terraform fejle med at subscription ID ikke kan bestemmes):

```powershell
az account list -o table
az account set --subscription <subscription-id>
```

- GHCR credentials (PAT med `read:packages`) så AKS kan trække images

## Konfiguration

Variabler styres via `terraform.tfvars`.

1. Kopiér eksempel-filen:

   - Se [terraform.tfvars.example](./terraform.tfvars.example)
   - Opret `terraform.tfvars` i samme mappe og udfyld værdierne

2. Vigtige felter:

- `location`, `environment`, `project_name`
- Postgres admin credentials
- GHCR credentials (`ghcr_username`, `ghcr_token`, `github_repository_owner`)
- `image_tag` (typisk `latest` eller commit SHA)

Commit aldrig `terraform.tfvars`.

## Grafana provisioning (Azure)

Azure Managed Grafana kan ikke provisioneres via fil-mount som i jeres lokale Grafana.

- I CI/CD kører vi provisioning via `terraform/azure/provision_grafana.ps1` (se workflows ovenfor).
- Hvis du kører manuelt, så kør først `terraform apply`, og derefter provisioning-scriptet med outputs fra Terraform.

## Kørsel uden script (manuel)

```powershell
cd .\terraform\azure
terraform init
terraform apply -auto-approve
```

Slet:

```powershell
terraform destroy -auto-approve
```

## Filer

| Fil                                                    | Beskrivelse                                        |
| ------------------------------------------------------ | -------------------------------------------------- |
| [main.tf](./main.tf)                                   | Providers + kobling til AKS + mtogo-app modulet    |
| [resources.tf](./resources.tf)                         | Azure ressourcer (RG, AKS, PostgreSQL + databases) |
| [variables.tf](./variables.tf)                         | Input variables                                    |
| [outputs.tf](./outputs.tf)                             | Outputs (fx kubeconfig/connection info)            |
| [terraform.tfvars.example](./terraform.tfvars.example) | Eksempel på variabel-fil                           |

## SLO Grafana (Azure)

Terraform opretter en **Azure Managed Grafana** instans dedikeret til SLO-visualisering.

- Endpoint output: `slo_grafana_endpoint`
- Azure Monitor Workspace output: `azure_monitor_workspace_id`

Adgang styres via **Entra ID + Azure RBAC**.

- Ops/consultancy (dem der driver sitet) får **Grafana Admin** på SLO-instansen via `grafana_slo_admin_principal_ids`.

## KPI Grafana (Azure)

Terraform opretter også en separat **Azure Managed Grafana** instans dedikeret til KPI-visualisering.

- Endpoint output: `kpi_grafana_endpoint`

Adgang styres via **Entra ID + Azure RBAC**.

- Business analysts får typisk **Grafana Viewer** via `grafana_kpi_viewer_principal_ids`.
- Hvis I vil tillade redigering af KPI dashboards, kan I tilføje **Grafana Editor** via `grafana_kpi_editor_principal_ids`.

## Alerting (Azure)

I Azure-setup’et bruger vi **Grafana Alerting** (i Azure Managed Grafana) sammen med metrics fra **Azure Monitor Workspace (Managed Prometheus)**.

- Der er derfor **ingen Alertmanager UI endpoint** i dette setup.
- For alerts: åbn KPI Grafana (`kpi_grafana_endpoint`) og brug menuen **Alerting**.

### Dashboard JSON

Dashboardet ligger i repoet her:

- [monitoring-infrastructure/grafana/dashboards/mtogo-slo-dashboard.json](../../monitoring-infrastructure/grafana/dashboards/mtogo-slo-dashboard.json)

Bemærk: Azure Managed Grafana kan ikke provisioneres via mount af filer som jeres lokale Grafana.
Importér derfor dashboard JSON manuelt i Grafana UI (Dashboards → New → Import) eller via Grafana API.
