# Terraform

Infrastructure-as-Code til MToGo-platformen. Terraform kan deploye enten:

- **Local**: Kubernetes (typisk Docker Desktop Kubernetes / Minikube)
- **Azure**: AKS + Azure Database for PostgreSQL (Flexible Server)

Deployment kører via PowerShell-scriptet [deploy.ps1](./deploy.ps1), som wrapper `terraform init/apply/destroy` og guider dig gennem de typiske fejlscenarier.

## Hurtig start

Kør kommandoerne fra repository root.

### Local (Kubernetes)

Deploy til din lokale Kubernetes-kontekst og byg images lokalt:

```powershell
.\terraform\deploy.ps1 -Context local -Build
```

Deploy uden build (forudsætter at images allerede findes lokalt):

```powershell
.\terraform\deploy.ps1 -Context local
```

Slet deployment:

```powershell
.\terraform\deploy.ps1 -Context local -Destroy
```

Efter et succesfuldt local-deploy skriver scriptet bl.a. disse endpoints ud:

- Website: `http://localhost/`
- API: `http://localhost/api/v1/`
- Legacy API: `http://localhost/legacy`

### Azure (AKS)

Deploy til Azure:

```powershell
.\terraform\deploy.ps1 -Context azure
```

Slet Azure deployment:

```powershell
.\terraform\deploy.ps1 -Context azure -Destroy
```

Bemærk: Azure deploy bygger **ikke** lokale Docker images. Services forventes at blive hentet fra **GHCR** (GitHub Container Registry).

## Forudsætninger

### Generelt

- Terraform installeret og tilgængelig på PATH (`terraform`)
- PowerShell (Windows PowerShell 5.1 eller PowerShell 7+)

### Local (Kubernetes)

- `kubectl` installeret
- En lokal Kubernetes-kontekst (fx `docker-desktop` eller `minikube`)
- Hvis du bruger `-Build`: Docker installeret og kørende (Linux containers)

Scriptet forsøger at vælge `docker-desktop` automatisk hvis den findes, og stopper hvis din nuværende `kubectl` context ligner AKS (for at undgå at du deployer “local” til cloud ved en fejl).

### Azure

- Azure subscription
- En måde at autentificere Terraform på (typisk via Azure CLI `az login` eller Service Principal)
- GHCR credentials (PAT med `read:packages`) til at kunne trække container images

## Konfiguration

Hver context har sin egen Terraform mappe og variabler.

### Local

Eksempel: [local/terraform.tfvars.example](./local/terraform.tfvars.example)

- Hvis du vil ændre defaults, kopier filen til `terraform/local/terraform.tfvars` og tilpas værdierne.
- Standard image settings er `image_registry = "mtogo"` og `image_tag = "latest"`, som matcher `-Build`-flowet.

### Azure

Eksempel: [azure/terraform.tfvars.example](./azure/terraform.tfvars.example)

- Kopiér til `terraform/azure/terraform.tfvars` og udfyld:
  - `location`, `environment`, `project_name`
  - Postgres admin credentials
  - GHCR credentials (`ghcr_username`, `ghcr_token`, `github_repository_owner`)
  - `image_tag` (ofte `latest` eller et commit SHA)

Vigtigt: Commit aldrig `terraform.tfvars` (indeholder secrets).

## Mappestruktur

| Mappe / fil                                | Beskrivelse                                                                 |
| ------------------------------------------ | --------------------------------------------------------------------------- |
| [deploy.ps1](./deploy.ps1)                 | Wrapper til `terraform init/apply/destroy` for local/azure                  |
| [local/](./local/)                         | Local Kubernetes deployment (k8s manifests via Terraform)                   |
| [azure/](./azure/)                         | Azure deployment (AKS + PostgreSQL Flexible Server)                         |
| [modules/](./modules/)                     | Genbrugelige Terraform-moduler                                              |
| [modules/mtogo-app/](./modules/mtogo-app/) | Modulet der beskriver MToGo app resources (services/ingress/infrastructure) |
