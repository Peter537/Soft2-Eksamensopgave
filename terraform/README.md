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
