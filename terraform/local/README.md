# Terraform (Local)

Terraform-konfiguration til at deploye MToGo-platformen til en lokal Kubernetes cluster.

Denne context:

- Bruger `kubernetes` + `helm` providers mod din nuværende `kubectl` context
- Deploy’er MToGo via modulet [modules/mtogo-app](../modules/mtogo-app/)
- Kan (valgfrit) installere NGINX Ingress Controller
- Deploy’er typisk PostgreSQL + Kafka **i clusteren** til lokal udvikling

## Hurtig start

Kør fra repository root (anbefalet):

```powershell
.\terraform\deploy.ps1 -Context local -Build
```

Deploy uden build:

```powershell
.\terraform\deploy.ps1 -Context local
```

Slet deployment:

```powershell
.\terraform\deploy.ps1 -Context local -Destroy
```

## Forudsætninger

- Terraform installeret (`terraform` på PATH)
- `kubectl` installeret
- En lokal Kubernetes context (fx `docker-desktop` eller `minikube`)
- Hvis du bruger `-Build`: Docker installeret og kørende (Linux containers)

## Konfiguration

Variabler styres via `terraform.tfvars`.

- Se [terraform.tfvars.example](./terraform.tfvars.example)
- Opret `terraform.tfvars` i samme mappe hvis du vil ændre defaults

Eksempel på relevante værdier:

- `image_registry` og `image_tag` (matcher `deploy.ps1 -Build` images: `mtogo/<service>:latest`)
- Postgres credentials til den lokale in-cluster PostgreSQL
- `install_ingress` (installer ingress-nginx via Helm)
- `environment` (default: `dev`)
- `discord_webhook_url` (valgfri; hvis sat, sender Alertmanager alerts til Discord)
- Seeded ManagementService admin credentials

Commit aldrig `terraform.tfvars`.

## Kørsel uden script (manuel)

```powershell
cd .\terraform\local
terraform init
terraform apply -auto-approve
```

Slet:

```powershell
terraform destroy -auto-approve
```

## Adgang

Ved ingress (default) routes trafikken typisk sådan:

- Website: `http://localhost/`
- API: `http://localhost/api/v1/`
- Legacy API: `http://localhost/legacy`

Monitoring installeres også i clusteren (for at matche de andre startup-modes):

- Grafana (KPI): `http://localhost:3000`
- Grafana (SLO): `http://localhost:3001`
- Prometheus: `http://localhost:9090`
- Alertmanager: `http://localhost:9093`

Hvis `discord_webhook_url` ikke er sat, kører Alertmanager stadig, men sender ikke alerts til Discord.

Bemærk: Den mest opdaterede liste udskrives af `deploy.ps1` via Terraform outputs.

## Filer

| Fil                                                    | Beskrivelse                                       |
| ------------------------------------------------------ | ------------------------------------------------- |
| [main.tf](./main.tf)                                   | Providers + mtogo-app modul (local konfiguration) |
| [variables.tf](./variables.tf)                         | Input variables                                   |
| [outputs.tf](./outputs.tf)                             | Outputs                                           |
| [terraform.tfvars.example](./terraform.tfvars.example) | Eksempel på variabel-fil                          |
