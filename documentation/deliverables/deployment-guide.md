Lokal opsætning af Azure CLI med Python 3.11:

```powershell
py -3.11 -m venv venv311
venv311\Scripts\Activate.ps1
pip install azure-cli
```

Kørsel af Lokal Deployment Terraform:

Kopier filen `terraform\local\terraform.tfvars.example` til `terraform\local\terraform.tfvars` og tilpas den efter behov.

Vær i Root-mappen af projektet og kør:

```powershell
.\terraform\deploy.ps1 -Context local -Build
```

For at destruere deployment, kør:

```powershell
.\terraform\deploy.ps1 -Context local -Destroy
```

Applikationen vil være tilgængelig på følgende URL'er efter deployment:

- Website: http://localhost/
- API: http://localhost/api/v1/
- Legacy API: http://localhost/legacy

Husk at have Docker Desktop kørende, da Kubernetes clusteret benytter Docker som container runtime.

# Terraform

Denne mappe indeholder Terraform-konfigurationer til både **lokal** og **Azure** deployment.

## Forudsætninger

### Lokal (`local`)

- Docker Desktop (med **Kubernetes** slået til)
- `kubectl`
- `terraform`

### Azure (`azure`)

- Azure CLI (`az`) og `az login`
- `terraform`

Derudover skal du udfylde variablerne i `terraform/azure/terraform.tfvars` (kopiér fra `terraform/azure/terraform.tfvars.example`).

## Kørsel fra root-mappen

Alle kommandoer køres fra projektets root-mappe.

### Lokal deployment

Byg Docker images lokalt og deploy til lokal Kubernetes:

```powershell
.\terraform\deploy.ps1 -Context local -Build
```

Deploy uden at bygge images igen:

```powershell
.\terraform\deploy.ps1 -Context local
```

Destroy lokal deployment:

```powershell
.\terraform\deploy.ps1 -Context local -Destroy
```

### Azure deployment

Deploy til Azure (AKS + app deploy fra GHCR):

```powershell
.\terraform\deploy.ps1 -Context azure
```

Hvis du bruger `-Build` på Azure, kører scriptet stadig Terraform-deploy (der bygges ikke lokale Docker images):

```powershell
.\terraform\deploy.ps1 -Context azure -Build
```

Destroy Azure deployment:

```powershell
.\terraform\deploy.ps1 -Context azure -Destroy
```

## Image-kilde (lokal vs Azure)

- **Lokal**: Docker images bygges lokalt fra Dockerfiles og deployes som `mtogo/<service>:latest`.
- **Azure**: Kubernetes henter images fra **GHCR** (`ghcr.io/<owner>/<service>:<tag>`). Images pushes typisk via CI/CD, ikke fra denne script.
