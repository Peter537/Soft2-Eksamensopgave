# Terraform (Azure)

Terraform-konfiguration til at deploye MToGo-platformen til Azure.

Denne context provisionerer:

- **AKS** (Azure Kubernetes Service)
- **Azure Database for PostgreSQL Flexible Server** (+ databases pr. service)
- **Kubernetes-resources** via modulet [modules/mtogo-app](../modules/mtogo-app/)

## Hurtig start

Kør fra repository root (anbefalet):

```powershell
.\terraform\deploy.ps1 -Context azure
```

Slet alt igen:

```powershell
.\terraform\deploy.ps1 -Context azure -Destroy
```

## Forudsætninger

- Terraform installeret (`terraform` på PATH)
- Azure subscription
- Azure autentifikation (fx via Azure CLI):

```powershell
az login
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
