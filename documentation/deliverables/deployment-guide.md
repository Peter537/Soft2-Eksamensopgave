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
