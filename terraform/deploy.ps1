# ===========================================
# MToGo Terraform Deployment Script
# ===========================================
# Deploys either Local Kubernetes or Azure (AKS) using Terraform.
#
# Usage (from repository root):
#   .\terraform\deploy.ps1 -Context local -Build
#   .\terraform\deploy.ps1 -Context local -Destroy
#   .\terraform\deploy.ps1 -Context azure
#   .\terraform\deploy.ps1 -Context azure -Destroy

param(
    [ValidateSet("local", "azure")]
    [string]$Context = "local",
    [switch]$Build,
    [switch]$Destroy
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Improve how PowerShell decodes output from native commands like Terraform.
# (Avoids garbled box-drawing chars on Windows PowerShell.)
try {
    $OutputEncoding = [System.Text.Encoding]::UTF8
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
} catch {
    # Best-effort only
}

function Fail {
    param(
        [Parameter(Mandatory = $true)][string]$Message
    )

    throw $Message
}

function Invoke-TerraformInDir {
    param(
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $prevEA = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    function Quote-ForCmd {
        param([Parameter(Mandatory = $true)][string]$Value)

        if ($Value -match '[\s&()^%=!\"]') {
            $escaped = $Value -replace '"', '""'
            return '"' + $escaped + '"'
        }

        return $Value
    }

    Push-Location $WorkingDirectory
    try {
        $tee = @()

        # Stream output live AND keep a copy for parsing.
        # Use cmd.exe to merge stderr into stdout so Windows PowerShell doesn't emit NativeCommandError records.
        $cmdArgs = ($Arguments | ForEach-Object { Quote-ForCmd $_ }) -join ' '
        $cmdLine = 'terraform ' + $cmdArgs + ' 2>&1'
        & cmd.exe /d /c $cmdLine | Tee-Object -Variable tee | Out-Host
        $exitCode = $LASTEXITCODE
        $lines = @($tee | ForEach-Object { $_.ToString() })

        return [pscustomobject]@{
            ExitCode = $exitCode
            Lines    = $lines
            Output   = ($lines -join "`n")
        }
    }
    finally {
        Pop-Location
        $ErrorActionPreference = $prevEA
    }
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  MToGo Terraform Deployment ($Context)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

if (-not (Get-Command terraform -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Terraform is not installed" -ForegroundColor Red
    Write-Host "Install from: https://www.terraform.io/downloads" -ForegroundColor Yellow
    Fail "Terraform is not installed"
}

$tfDir = Join-Path $RootDir "terraform\$Context"
if (-not (Test-Path $tfDir)) {
    Write-Host "Error: Terraform folder not found: $tfDir" -ForegroundColor Red
    Fail "Terraform folder not found: $tfDir"
}

if ($Context -eq "local") {
    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        Write-Host "Error: kubectl is not installed" -ForegroundColor Red
        Fail "kubectl is not installed"
    }

    # Prefer docker-desktop if available (common on Windows)
    $availableContexts = kubectl config get-contexts -o name 2>$null
    if (-not $availableContexts) {
        Write-Host "Error: No Kubernetes contexts found" -ForegroundColor Red
        Write-Host "Make sure Docker Desktop Kubernetes is enabled or Minikube is running" -ForegroundColor Yellow
        Fail "No Kubernetes contexts found"
    }

    if ($availableContexts -contains "docker-desktop") {
        kubectl config use-context docker-desktop 1>$null 2>$null
    }

    $kubeContext = kubectl config current-context 2>$null
    if (-not $kubeContext) {
        Write-Host "Error: No Kubernetes context selected" -ForegroundColor Red
        Fail "No Kubernetes context selected"
    }

    if ($kubeContext -match "aks") {
        Write-Host "Error: Current kubectl context looks like AKS: $kubeContext" -ForegroundColor Red
        Write-Host "Switch to a local context (e.g., 'docker-desktop') before deploying local." -ForegroundColor Yellow
        Write-Host "Available contexts:" -ForegroundColor Yellow
        $availableContexts | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
        Fail "Current kubectl context looks like AKS: $kubeContext"
    }

    Write-Host "Using Kubernetes context: $kubeContext" -ForegroundColor Green

    if ($Build) {
        Write-Host "`nBuilding Docker images..." -ForegroundColor Yellow

        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            Write-Host "Error: docker is not installed" -ForegroundColor Red
            Fail "docker is not installed"
        }

        docker version 1>$null 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error: Docker engine is not reachable" -ForegroundColor Red
            Write-Host "Start Docker Desktop and ensure it is using Linux containers." -ForegroundColor Yellow
            Fail "Docker engine is not reachable"
        }

        $registry = "mtogo"
        $tag = "latest"

        $builds = @(
            @{ Name = "mtogo-gateway";            Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.Gateway\Dockerfile" },
            @{ Name = "mtogo-website";            Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.Website\Dockerfile" },
            @{ Name = "mtogo-order";              Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.OrderService\Dockerfile" },
            @{ Name = "mtogo-customerservice";    Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.CustomerService\Dockerfile" },
            @{ Name = "mtogo-agentservice";       Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.AgentService\Dockerfile" },
            @{ Name = "mtogo-agentbonus";         Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.AgentBonusService\Dockerfile" },
            @{ Name = "mtogo-feedbackhub";        Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.FeedbackHubService\Dockerfile" },
            @{ Name = "mtogo-notification";       Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.NotificationService\Dockerfile" },
            @{ Name = "mtogo-partner";            Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.PartnerService\Dockerfile" },
            @{ Name = "mtogo-websocketagent";     Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.WebSocketAgentService\Dockerfile" },
            @{ Name = "mtogo-websocketcustomer";  Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.WebSocketCustomerService\Dockerfile" },
            @{ Name = "mtogo-websocketpartner";   Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.WebSocketPartnerService\Dockerfile" },
            @{ Name = "mtogo-management";         Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.ManagementService\Dockerfile" },
            @{ Name = "mtogo-logcollector";       Context = "$RootDir\MToGo";       Dockerfile = "$RootDir\MToGo\src\MToGo.LogCollectorService\Dockerfile" },
            @{ Name = "mtogo-legacy";             Context = "$RootDir\LegacyMToGo"; Dockerfile = "$RootDir\LegacyMToGo\Dockerfile" }
        )

        foreach ($b in $builds) {
            $image = "$registry/$($b.Name):$tag"
            Write-Host "Building $image" -ForegroundColor Cyan
            docker build -t $image -f $b.Dockerfile $b.Context
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error building $image" -ForegroundColor Red
                Fail "Error building $image"
            }
        }
    }
}

if ($Context -eq "azure" -and $Build) {
    Write-Host "`nBuild flag set for Azure." -ForegroundColor Yellow
    Write-Host "Azure deploy uses Terraform and pulls images from GHCR (no local Docker build step)." -ForegroundColor Yellow
}

if ($Destroy) {
    Write-Host "`nDestroying $Context deployment..." -ForegroundColor Yellow
    Set-Location $tfDir
    terraform destroy -auto-approve
    Write-Host "`nDeployment destroyed!" -ForegroundColor Green
    return
}

Write-Host "`nInitializing Terraform..." -ForegroundColor Yellow
Set-Location $tfDir

$maxInitAttempts = 6
$initSucceeded = $false
for ($i = 1; $i -le $maxInitAttempts; $i++) {
    if ($i -gt 1) {
        Write-Host "`nRetrying Terraform init (attempt $i/$maxInitAttempts)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }

    $initResult = Invoke-TerraformInDir -WorkingDirectory $tfDir -Arguments @("init")
    if ($initResult.ExitCode -eq 0) {
        $initSucceeded = $true
        break
    }

    $locked = ($initResult.Output -match "locked" -or $initResult.Output -match "låst" -or $initResult.Output -match "cannot get access" -or $initResult.Output -match "Processen kan ikke få adgang")
    if ($locked) {
        Write-Host "`nTerraform state appears to be locked by another process." -ForegroundColor Yellow
        Write-Host "Close other running Terraform commands/terminals and try again." -ForegroundColor Yellow
        $tfProcs = Get-Process terraform -ErrorAction SilentlyContinue
        if ($tfProcs) {
            Write-Host "Running terraform processes:" -ForegroundColor Yellow
            $tfProcs | ForEach-Object { Write-Host ("  - Id={0}  StartTime={1}" -f $_.Id, (try { $_.StartTime } catch { "?" })) -ForegroundColor White }
        }
        continue
    }

    Write-Host "Error: Terraform init failed" -ForegroundColor Red
    Fail "Terraform init failed"
}

if (-not $initSucceeded) {
    Write-Host "Error: Terraform init failed (state remained locked)" -ForegroundColor Red
    Fail "Terraform init failed (state remained locked)"
}

Write-Host "`nApplying Terraform configuration..." -ForegroundColor Yellow

$maxApplyAttempts = 10
$applySucceeded = $false
for ($attempt = 1; $attempt -le $maxApplyAttempts; $attempt++) {
    if ($attempt -gt 1) {
        Write-Host "`nRetrying Terraform apply (attempt $attempt/$maxApplyAttempts)..." -ForegroundColor Yellow
    }

    Write-Host "(Terraform apply is running; this can take several minutes...)" -ForegroundColor Yellow
    $applyResult = Invoke-TerraformInDir -WorkingDirectory $tfDir -Arguments @("apply", "-auto-approve")

    if ($applyResult.ExitCode -eq 0) {
        $applySucceeded = $true
        break
    }

    if ($Context -ne "azure") {
        Write-Host "Error: Terraform apply failed" -ForegroundColor Red
        Fail "Terraform apply failed"
    }

    $outputLines = $applyResult.Lines
    $idMatch = $outputLines | Select-String -Pattern 'A resource with the ID "(?<id>[^"]+)" already exists' -AllMatches | Select-Object -First 1
    $addrMatch = $outputLines | Select-String -Pattern 'with (?<addr>[^,]+),' -AllMatches | Select-Object -First 1

    if ($idMatch -and $addrMatch) {
        $resourceId = $idMatch.Matches[0].Groups['id'].Value
        $resourceAddr = $addrMatch.Matches[0].Groups['addr'].Value

        Write-Host "`nDetected existing Azure resource that must be imported into Terraform state:" -ForegroundColor Yellow
        Write-Host "  Address: $resourceAddr" -ForegroundColor White
        Write-Host "  ID:      $resourceId" -ForegroundColor White
        Write-Host "Importing..." -ForegroundColor Yellow

        $importArgs = @(
            "import",
            $resourceAddr,
            $resourceId
        )
        Write-Host "(Terraform import is running...)" -ForegroundColor Yellow
        $importResult = Invoke-TerraformInDir -WorkingDirectory $tfDir -Arguments $importArgs
        if ($importResult.ExitCode -ne 0) {
            Write-Host "Error: Terraform import failed" -ForegroundColor Red
            Fail "Terraform import failed"
        }

        continue
    }

    Write-Host "Error: Terraform apply failed" -ForegroundColor Red
    Fail "Terraform apply failed"
}

if (-not $applySucceeded) {
    Write-Host "Error: Terraform apply failed after $maxApplyAttempts attempts" -ForegroundColor Red
    Fail "Terraform apply failed after $maxApplyAttempts attempts"
}

if ($Context -eq "local") {
    Write-Host "`nWaiting for deployments to be ready..." -ForegroundColor Yellow
    kubectl wait --for=condition=available --timeout=300s deployment --all -n mtogo

    Write-Host "`n================================================" -ForegroundColor Green
    Write-Host "  Deployment Complete!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green

    Write-Host "`nPod Status:" -ForegroundColor Cyan
    kubectl get pods -n mtogo

    Write-Host "`nAccess the application:" -ForegroundColor Cyan
    Write-Host "  Website:    http://localhost/" -ForegroundColor White
    Write-Host "  API:        http://localhost/api/v1/" -ForegroundColor White
    Write-Host "  Legacy API: http://localhost/legacy" -ForegroundColor White
}
