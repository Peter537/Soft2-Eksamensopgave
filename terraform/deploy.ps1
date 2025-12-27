# ===========================================
# MToGo Terraform Deployment Script
# ===========================================
# Deploys either Local Kubernetes or Azure (AKS) using Terraform.
#
# Usage (from repository root):
#   .\terraform\deploy.ps1 -Context local -Build
#   .\terraform\deploy.ps1 -Context local -Destroy
#   .\terraform\deploy.ps1 -Context local -Build -SeedDemoData
#   .\terraform\deploy.ps1 -Context azure
#   .\terraform\deploy.ps1 -Context azure -Destroy
#   .\terraform\deploy.ps1 -Context azure -SeedDemoData

param(
    [ValidateSet("local", "azure")]
    [string]$Context = "local",
    [switch]$Build,
    [switch]$Destroy,

    # When set, Terraform will run an in-cluster job that seeds demo data into the databases.
    [switch]$SeedDemoData,

    # Local-only: when set, provision Grafana-managed alerting (Discord contact point + import KPI alert rules)
    # in addition to Prometheus + Alertmanager. Default is off to avoid Grafana 'DatasourceNoData' alerts.
    [switch]$ProvisionLocalGrafanaAlerting,

    # Azure-only: when set, provision Azure Managed Grafana alerting (Discord contact point + import KPI alert rules).
    # Default is off so Discord only receives Alertmanager template embeds from in-cluster Alertmanager.
    [switch]$ProvisionAzureGrafanaAlerting
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

function Get-TerraformOutputs {
    param(
        [Parameter(Mandatory = $true)][string]$WorkingDirectory
    )

    $result = Invoke-TerraformInDir -WorkingDirectory $WorkingDirectory -Arguments @("output", "-json")
    if ($result.ExitCode -ne 0) {
        Fail "Failed to read Terraform outputs"
    }

    try {
        return ($result.Output | ConvertFrom-Json)
    }
    catch {
        Fail "Failed to parse Terraform outputs as JSON"
    }
}

function Write-EndpointSection {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)]$Map
    )

    Write-Host "`n$Title" -ForegroundColor Cyan
    $keys = @($Map.PSObject.Properties.Name | Sort-Object)
    foreach ($k in $keys) {
        $v = $Map.$k
        if ($null -ne $v -and "$v".Trim().Length -gt 0) {
            Write-Host ("  {0}: {1}" -f $k, $v) -ForegroundColor White
        }
    }
}

function Ensure-AzureSubscription {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Host "Error: Azure CLI (az) is not installed" -ForegroundColor Red
        Write-Host "Install from: https://learn.microsoft.com/cli/azure/install-azure-cli" -ForegroundColor Yellow
        Fail "Azure CLI is not installed"
    }

    # Verify login / get active subscription
    $subId = ""
    $tenantId = ""
    try {
        $subId = (az account show --query id -o tsv 2>$null).Trim()
        $tenantId = (az account show --query tenantId -o tsv 2>$null).Trim()
    }
    catch {
        $subId = ""
        $tenantId = ""
    }

    if (-not $subId) {
        Write-Host "Error: Azure subscription could not be determined." -ForegroundColor Red
        Write-Host "Fix:" -ForegroundColor Yellow
        Write-Host "  1) Run: az login" -ForegroundColor White
        Write-Host "  2) If you have multiple subscriptions, run: az account set --subscription <subscription-id>" -ForegroundColor White
        Write-Host "  3) Re-run this script." -ForegroundColor White
        Write-Host "Hint: list subscriptions with: az account list -o table" -ForegroundColor Yellow
        Fail "Azure subscription could not be determined"
    }

    # Export ARM_* so Terraform's azurerm provider always has subscription context.
    $env:ARM_SUBSCRIPTION_ID = $subId
    if ($tenantId) {
        $env:ARM_TENANT_ID = $tenantId
    }

    Write-Host "Using Azure subscription: $subId" -ForegroundColor Green
}

function Ensure-AzureResourceProviders {
    param(
        [Parameter(Mandatory = $true)][string[]]$Namespaces
    )

    foreach ($ns in $Namespaces) {
        $state = ""
        try {
            $state = (az provider show -n $ns --query registrationState -o tsv 2>$null).Trim()
        }
        catch {
            $state = ""
        }

        if (-not $state) {
            Write-Host "Error: Failed to query provider registration state for '${ns}'." -ForegroundColor Red
            Write-Host "Make sure Azure CLI is logged in and you have access to the subscription." -ForegroundColor Yellow
            Fail "Failed to query provider registration state for '${ns}'"
        }

        if ($state -eq "Registered") {
            continue
        }

        Write-Host "Registering Azure resource provider '${ns}' (current: $state)..." -ForegroundColor Yellow
        az provider register -n $ns 1>$null 2>$null

        # Poll until it becomes Registered.
        $maxPolls = 60
        for ($i = 1; $i -le $maxPolls; $i++) {
            Start-Sleep -Seconds 5
            $newState = (az provider show -n $ns --query registrationState -o tsv 2>$null).Trim()
            if ($newState -eq "Registered") {
                Write-Host "Provider '${ns}' is now Registered." -ForegroundColor Green
                break
            }

            if ($i -eq $maxPolls) {
                Write-Host "Error: Provider '${ns}' did not become Registered in time (last state: $newState)." -ForegroundColor Red
                Write-Host "If this keeps happening, try manually:" -ForegroundColor Yellow
                Write-Host "  az provider register -n ${ns}" -ForegroundColor White
                Write-Host "  az provider show -n ${ns} --query registrationState -o tsv" -ForegroundColor White
                Fail "Provider '${ns}' registration timed out"
            }
        }
    }
}

function Ensure-AzureCliExtension {
    param(
        [Parameter(Mandatory = $true)][string]$Name
    )

    $null = az extension show -n $Name 2>$null
    if ($LASTEXITCODE -eq 0) {
        return
    }

    Write-Host "Installing Azure CLI extension '${Name}'..." -ForegroundColor Yellow
    az extension add -n $Name 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        Fail "Failed to install Azure CLI extension '${Name}'"
    }
}

function Invoke-AzCliWithTimeout {
    param(
        [Parameter(Mandatory = $true)][string[]]$Args,
        [int]$TimeoutSeconds = 300,
        [switch]$AllowFailure
    )

    function Quote-AzArg {
        param([Parameter(Mandatory = $true)][string]$Value)

        if ($Value -match '"') {
            $Value = $Value -replace '"', '\\"'
        }
        if ($Value -match '\s') {
            return '"' + $Value + '"'
        }
        return $Value
    }

    $azCmd = $null
    try {
        $azCmd = (Get-Command az -ErrorAction SilentlyContinue).Source
    }
    catch {
        $azCmd = $null
    }

    if (-not $azCmd) {
        Fail "Azure CLI (az) was not found on PATH"
    }

    $argString = ($Args | ForEach-Object { Quote-AzArg $_ }) -join ' '
    Write-Host ("Running: az {0}" -f $argString) -ForegroundColor DarkGray

    # Avoid classic deadlocks where the child process fills stdout/stderr pipes before exiting.
    # Instead, redirect cmd.exe output to files and read them after.
    $tempRoot = Join-Path $env:TEMP "mtogo-azcli"
    if (-not (Test-Path $tempRoot)) {
        New-Item -ItemType Directory -Path $tempRoot | Out-Null
    }

    $id = [Guid]::NewGuid().ToString("N")
    $outFile = Join-Path $tempRoot ("az-" + $id + ".out.txt")
    $errFile = Join-Path $tempRoot ("az-" + $id + ".err.txt")

    # cmd.exe expects: /c ""C:\path\az.cmd" args... 1>out 2>err"
    $cmdLine = '"' + $azCmd + '" ' + $argString + ' 1>"' + $outFile + '" 2>"' + $errFile + '"'

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "cmd.exe"
    $psi.Arguments = "/d /s /c `"$cmdLine`""
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $null = $proc.Start()

    if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
        try { $proc.Kill() } catch { }
        $sw.Stop()

        $partialOut = ""
        $partialErr = ""
        try { if (Test-Path $outFile) { $partialOut = (Get-Content -Raw -Path $outFile -Encoding UTF8 -ErrorAction SilentlyContinue) } } catch { }
        try { if (Test-Path $errFile) { $partialErr = (Get-Content -Raw -Path $errFile -Encoding UTF8 -ErrorAction SilentlyContinue) } } catch { }

        if ($partialOut) { Write-Host $partialOut -ForegroundColor White }
        if ($partialErr) { Write-Host $partialErr -ForegroundColor Yellow }

        Fail ("Azure CLI timed out after {0}s: az {1}" -f $TimeoutSeconds, $argString)
    }

    $sw.Stop()
    $stdout = ""
    $stderr = ""
    try { if (Test-Path $outFile) { $stdout = (Get-Content -Raw -Path $outFile -Encoding UTF8 -ErrorAction SilentlyContinue) } } catch { $stdout = "" }
    try { if (Test-Path $errFile) { $stderr = (Get-Content -Raw -Path $errFile -Encoding UTF8 -ErrorAction SilentlyContinue) } } catch { $stderr = "" }

    # Cleanup best-effort.
    try { Remove-Item -Path $outFile -Force -ErrorAction SilentlyContinue } catch { }
    try { Remove-Item -Path $errFile -Force -ErrorAction SilentlyContinue } catch { }

    if ($proc.ExitCode -ne 0 -and -not $AllowFailure) {
        if ($stdout) { Write-Host $stdout -ForegroundColor White }
        if ($stderr) { Write-Host $stderr -ForegroundColor Yellow }
        Fail ("Azure CLI failed (exit {0}) after {1}s: az {2}" -f $proc.ExitCode, [math]::Round($sw.Elapsed.TotalSeconds, 1), $argString)
    }

    return [pscustomobject]@{
        ExitCode = $proc.ExitCode
        StdOut   = $stdout
        StdErr   = $stderr
        Seconds  = $sw.Elapsed.TotalSeconds
    }
}

function Write-Utf8FileNoBom {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Value, $utf8NoBom)
}

function Get-TerraformTfvarsStringValue {
    param(
        [Parameter(Mandatory = $true)][string]$TerraformDirectory,
        [Parameter(Mandatory = $true)][string]$VariableName
    )

    $jsonTfvarsPath = Join-Path $TerraformDirectory "terraform.tfvars.json"
    if (Test-Path $jsonTfvarsPath) {
        try {
            $obj = (Get-Content -Raw -Path $jsonTfvarsPath -Encoding UTF8 | ConvertFrom-Json)
            if ($obj.PSObject.Properties.Name -contains $VariableName) {
                $v = $obj.$VariableName
                if ($null -ne $v -and ("$v").Trim().Length -gt 0) {
                    return ("$v").Trim()
                }
            }
        } catch {
            # ignore and fall back to HCL tfvars
        }
    }

    $hclTfvarsPath = Join-Path $TerraformDirectory "terraform.tfvars"
    if (-not (Test-Path $hclTfvarsPath)) {
        return $null
    }

    try {
        $content = Get-Content -Raw -Path $hclTfvarsPath -Encoding UTF8
    } catch {
        return $null
    }

    $escapedName = [regex]::Escape($VariableName)

    # Prefer the common tfvars style: key = "value" (double quotes)
    $patternDouble = '(?m)^\s*' + $escapedName + '\s*=\s*"(?<v>[^"]*)"\s*(#.*)?$'
    $m = [regex]::Match($content, $patternDouble)

    # Fall back to single quotes: key = 'value'
    if (-not $m.Success) {
        $patternSingle = '(?m)^\s*' + $escapedName + "\\s*=\\s*'(?<v>[^']*)'\\s*(#.*)?$"
        $m = [regex]::Match($content, $patternSingle)
    }

    if (-not $m.Success) {
        return $null
    }

    $value = $m.Groups['v'].Value
    if (-not $value -or $value.Trim().Length -eq 0) {
        return $null
    }

    return $value.Trim()
}

function Ensure-AzureManagedGrafanaKpiDiscordAlerting {
    param(
        [Parameter(Mandatory = $true)][string]$ResourceGroupName,
        [Parameter(Mandatory = $true)][string]$GrafanaName,
        [Parameter(Mandatory = $true)][string]$DiscordWebhookUrl
    )

    if (-not $DiscordWebhookUrl -or $DiscordWebhookUrl.Trim().Length -eq 0) {
        Fail "KPI alerting requires 'discord_webhook_url' to be set in terraform/azure/terraform.tfvars (or terraform.tfvars.json)."
    }

    $endpointResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "show",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "--query", "properties.endpoint",
        "-o", "tsv"
    ) -TimeoutSeconds 60
    $grafanaEndpoint = ($endpointResult.StdOut | Out-String).Trim()
    if (-not $grafanaEndpoint) {
        Fail "Failed to determine Grafana endpoint for '${GrafanaName}'."
    }

    # Use a short-lived service account token so we can call Grafana's HTTP API for alerting.
    # (Azure CLI doesn't currently expose unified alerting/contact-point provisioning.)
    $serviceAccountName = "mtogo-provisioner"

    $saListResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "service-account", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "-o", "json"
    ) -TimeoutSeconds 120

    $serviceAccounts = @()
    try { $serviceAccounts = ($saListResult.StdOut | ConvertFrom-Json) } catch { $serviceAccounts = @() }

    $existingSa = $serviceAccounts | Where-Object { $_.name -eq $serviceAccountName } | Select-Object -First 1
    if (-not $existingSa) {
        Invoke-AzCliWithTimeout -Args @(
            "grafana", "service-account", "create",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--service-account", $serviceAccountName,
            "--role", "Admin",
            "-o", "none"
        ) -TimeoutSeconds 180
    }

    $tokenName = "deploy-" + [Guid]::NewGuid().ToString("N")
    $tokenId = $null
    $apiToken = $null

    try {
        $tokenCreateResult = Invoke-AzCliWithTimeout -Args @(
            "grafana", "service-account", "token", "create",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--service-account", $serviceAccountName,
            "--token", $tokenName,
            "--time-to-live", "1h",
            "-o", "json"
        ) -TimeoutSeconds 180

        $tokenObj = $null
        try { $tokenObj = ($tokenCreateResult.StdOut | ConvertFrom-Json) } catch { $tokenObj = $null }
        if ($null -eq $tokenObj) {
            Fail "Failed to parse service account token create response from Azure CLI."
        }

        foreach ($prop in @("key", "token", "secret", "value")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $apiToken = $tokenObj.$prop
                break
            }
        }

        foreach ($prop in @("id", "tokenId", "uid")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $tokenId = $tokenObj.$prop
                break
            }
        }

        if (-not $apiToken) {
            Fail "Azure CLI did not return a usable service account token secret."
        }

        $headers = @{
            Authorization  = "Bearer $apiToken"
            "Content-Type" = "application/json"
            Accept         = "application/json"
        }

        $base = $grafanaEndpoint.TrimEnd('/')
        $contactPointName = "discord-business-alerts"

        # Grafana's provisioning API for contact points uses the flat schema:
        # { uid?, name, type, settings, disableResolveMessage }
        $cpBody = @{
            name                 = $contactPointName
            type                 = "discord"
            settings             = @{ url = $DiscordWebhookUrl }
            disableResolveMessage = $false
        }

        $existing = $null
        try {
            $existing = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/contact-points") -Headers $headers
        }
        catch {
            $msg = $_.Exception.Message
            if ($_.ErrorDetails.Message) { $msg = $msg + " `n" + $_.ErrorDetails.Message }
            Fail "Failed to query Grafana contact points on '${GrafanaName}'. Ensure alerting APIs are enabled and you have Grafana Admin rights. Details: $msg"
        }

        $existingList = @()
        try {
            # If the API returns a single object, wrap it; otherwise keep the array.
            $existingList = @($existing)
        } catch {
            $existingList = @()
        }

        $existingCp = $existingList | Where-Object { $_.name -eq $contactPointName } | Select-Object -First 1

        if ($existingCp -and ($existingCp.PSObject.Properties.Name -contains "uid") -and $existingCp.uid) {
            $cpBody.uid = $existingCp.uid
            $cpJson = ($cpBody | ConvertTo-Json -Depth 20)
            Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/contact-points/$($existingCp.uid)") -Headers $headers -Body $cpJson | Out-Null
        }
        else {
            $cpJson = ($cpBody | ConvertTo-Json -Depth 20)
            try {
                Invoke-RestMethod -Method Post -Uri ("$base/api/v1/provisioning/contact-points") -Headers $headers -Body $cpJson | Out-Null
            }
            catch {
                $msg = $_.Exception.Message
                if ($_.ErrorDetails.Message) { $msg = $msg + " `n" + $_.ErrorDetails.Message }
                Fail "Failed to create Grafana contact point '${contactPointName}' on '${GrafanaName}'. Details: $msg"
            }
        }

        # Route alerts to Discord by default (notification policy root receiver).
        try {
            $policy = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/policies") -Headers $headers
            if ($null -eq $policy) {
                $policy = @{}
            }

            # Preserve existing grouping if present.
            if (-not ($policy.PSObject.Properties.Name -contains 'group_by')) {
                $policy | Add-Member -NotePropertyName 'group_by' -NotePropertyValue @('grafana_folder', 'alertname')
            }

            $policy.receiver = $contactPointName
            $policyJson = ($policy | ConvertTo-Json -Depth 50)
            Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/policies") -Headers $headers -Body $policyJson | Out-Null
        }
        catch {
            $msg = $_.Exception.Message
            if ($_.ErrorDetails.Message) { $msg = $msg + " `n" + $_.ErrorDetails.Message }
            Fail "Failed to update Grafana notification policy on '${GrafanaName}' to use Discord contact point '${contactPointName}'. Details: $msg"
        }
    }
    finally {
        # Best-effort cleanup of the short-lived token.
        if ($tokenId) {
            Invoke-AzCliWithTimeout -Args @(
                "grafana", "service-account", "token", "delete",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--service-account", $serviceAccountName,
                "--token", $tokenId,
                "-o", "none"
            ) -TimeoutSeconds 120 -AllowFailure | Out-Null
        }
    }
}

function Remove-AzureManagedGrafanaKpiDiscordAlerting {
    param(
        [Parameter(Mandatory = $true)][string]$ResourceGroupName,
        [Parameter(Mandatory = $true)][string]$GrafanaName
    )

    $endpointResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "show",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "--query", "properties.endpoint",
        "-o", "tsv"
    ) -TimeoutSeconds 60 -AllowFailure

    if (-not $endpointResult -or $endpointResult.ExitCode -ne 0) {
        return
    }

    $grafanaEndpoint = ($endpointResult.StdOut | Out-String).Trim()
    if (-not $grafanaEndpoint) {
        return
    }

    $serviceAccountName = "mtogo-provisioner"

    $saListResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "service-account", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "-o", "json"
    ) -TimeoutSeconds 120 -AllowFailure

    if (-not $saListResult -or $saListResult.ExitCode -ne 0) {
        return
    }

    $serviceAccounts = @()
    try { $serviceAccounts = ($saListResult.StdOut | ConvertFrom-Json) } catch { $serviceAccounts = @() }
    $existingSa = $serviceAccounts | Where-Object { $_.name -eq $serviceAccountName } | Select-Object -First 1
    if (-not $existingSa) {
        return
    }

    $tokenName = "cleanup-" + [Guid]::NewGuid().ToString("N")
    $tokenId = $null
    $apiToken = $null

    try {
        $tokenCreateResult = Invoke-AzCliWithTimeout -Args @(
            "grafana", "service-account", "token", "create",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--service-account", $serviceAccountName,
            "--token", $tokenName,
            "--time-to-live", "1h",
            "-o", "json"
        ) -TimeoutSeconds 180 -AllowFailure

        if (-not $tokenCreateResult -or $tokenCreateResult.ExitCode -ne 0) {
            return
        }

        $tokenObj = $null
        try { $tokenObj = ($tokenCreateResult.StdOut | ConvertFrom-Json) } catch { $tokenObj = $null }
        if ($null -eq $tokenObj) {
            return
        }

        foreach ($prop in @("key", "token", "secret", "value")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $apiToken = $tokenObj.$prop
                break
            }
        }

        foreach ($prop in @("id", "tokenId", "uid")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $tokenId = $tokenObj.$prop
                break
            }
        }

        if (-not $apiToken) {
            return
        }

        $headers = @{
            Authorization  = "Bearer $apiToken"
            "Content-Type" = "application/json"
            Accept         = "application/json"
        }

        $base = $grafanaEndpoint.TrimEnd('/')
        $contactPointName = "discord-business-alerts"

        # Fetch contact points; if we don't see our Discord CP, nothing to do.
        $existingCps = $null
        try {
            $existingCps = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/contact-points") -Headers $headers
        }
        catch {
            return
        }

        $cpList = @($existingCps)
        $cp = $cpList | Where-Object { $_.name -eq $contactPointName } | Select-Object -First 1
        if (-not $cp) {
            return
        }

        # If the root notification policy routes to our Discord CP, redirect it first.
        try {
            $policy = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/policies") -Headers $headers
            if ($policy -and ($policy.PSObject.Properties.Name -contains 'receiver') -and $policy.receiver -eq $contactPointName) {
                $fallback = $null

                # Prefer Grafana's default email receiver when present.
                $fallbackCandidate = $cpList | Where-Object { $_.name -eq 'grafana-default-email' } | Select-Object -First 1
                if ($fallbackCandidate) {
                    $fallback = $fallbackCandidate.name
                }
                else {
                    $fallbackCandidate = $cpList | Where-Object { $_.name -and $_.name -ne $contactPointName } | Select-Object -First 1
                    if ($fallbackCandidate) {
                        $fallback = $fallbackCandidate.name
                    }
                }

                if ($fallback) {
                    $policy.receiver = $fallback
                }
                else {
                    # Last resort: remove receiver property so Grafana can fall back to its defaults.
                    $policy.PSObject.Properties.Remove('receiver')
                }

                Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/policies") -Headers $headers -Body ($policy | ConvertTo-Json -Depth 50) | Out-Null
            }
        }
        catch {
            # Best-effort only
        }

        # Delete the Discord contact point.
        try {
            if ($cp.PSObject.Properties.Name -contains 'uid' -and $cp.uid) {
                Invoke-RestMethod -Method Delete -Uri ("$base/api/v1/provisioning/contact-points/$($cp.uid)") -Headers $headers | Out-Null
            }
        }
        catch {
            # Best-effort only
        }
    }
    finally {
        if ($tokenId) {
            Invoke-AzCliWithTimeout -Args @(
                "grafana", "service-account", "token", "delete",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--service-account", $serviceAccountName,
                "--token", $tokenId,
                "-o", "none"
            ) -TimeoutSeconds 120 -AllowFailure | Out-Null
        }
    }
}

function New-StableGrafanaUid {
    param(
        [Parameter(Mandatory = $true)][string]$Prefix,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $slug = ($Name.ToLowerInvariant() -replace '[^a-z0-9_-]', '-')
    $slug = ($slug -replace '-{2,}', '-')
    $slug = $slug.Trim('-')
    if (-not $slug) {
        $slug = "rule"
    }

    $uid = ($Prefix + $slug)
    if ($uid.Length -gt 40) {
        $uid = $uid.Substring(0, 40)
    }

    return $uid
}

function Parse-PrometheusAlertRulesYml {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (-not (Test-Path $Path)) {
        Fail "Alert rules file not found: $Path"
    }

    $lines = @(Get-Content -Path $Path -Encoding UTF8)
    $groups = @()

    $currentGroup = $null
    $inRules = $false
    $i = 0
    while ($i -lt $lines.Count) {
        $line = $lines[$i]

        # Group start: "  - name: foo"
        $mGroup = [regex]::Match($line, '^\s*-\s*name:\s*(?<name>[^#\r\n]+)')
        if ($mGroup.Success -and ($line -match '^\s{2}-\s*name:')) {
            if ($currentGroup) {
                $groups += $currentGroup
            }
            $currentGroup = [pscustomobject]@{
                name     = $mGroup.Groups['name'].Value.Trim().Trim('"')
                interval = $null
                rules    = @()
            }
            $inRules = $false
            $i++
            continue
        }

        if ($currentGroup -and (-not $inRules)) {
            $mInterval = [regex]::Match($line, '^\s*interval:\s*(?<v>[^#\r\n]+)')
            if ($mInterval.Success) {
                $currentGroup.interval = $mInterval.Groups['v'].Value.Trim()
                $i++
                continue
            }

            if ($line -match '^\s*rules:\s*$') {
                $inRules = $true
                $i++
                continue
            }
        }

        if ($currentGroup -and $inRules) {
            # Rule start: "      - alert: Name"
            $mAlert = [regex]::Match($line, '^\s*-\s*alert:\s*(?<name>[^#\r\n]+)')
            if ($mAlert.Success -and ($line -match '^\s{6}-\s*alert:')) {
                $ruleName = $mAlert.Groups['name'].Value.Trim().Trim('"')
                $expr = $null
                $for = $null
                $labels = @{}
                $annotations = @{}

                $i++
                while ($i -lt $lines.Count) {
                    $l = $lines[$i]
                    # Next rule/group?
                    if ($l -match '^\s{6}-\s*alert:' -or $l -match '^\s{2}-\s*name:') {
                        break
                    }

                    $mFor = [regex]::Match($l, '^\s*for:\s*(?<v>[^#\r\n]+)')
                    if ($mFor.Success) {
                        $for = $mFor.Groups['v'].Value.Trim()
                        $i++
                        continue
                    }

                    # expr: | (multiline YAML literal block)
                    $mExprBlock = [regex]::Match($l, '^(?<indent>\s*)expr:\s*\|\s*$')
                    if ($mExprBlock.Success) {
                        $exprIndent = $mExprBlock.Groups['indent'].Value.Length
                        $blockIndent = $exprIndent + 2

                        $i++
                        $exprLines = @()
                        while ($i -lt $lines.Count) {
                            $el = $lines[$i]

                            # In YAML literal blocks, content lines must be indented more than the key.
                            # Stop as soon as indentation drops back to the rule level (e.g., 'for:', 'labels:', 'annotations:').
                            if ($el -match ('^\s{' + $blockIndent + ',}.*$')) {
                                $exprLines += (($el.TrimEnd()) -replace ('^\s{' + $blockIndent + '}'), '')
                                $i++
                                continue
                            }

                            break
                        }

                        $expr = ($exprLines -join "`n").Trim()
                        continue
                    }

                    # expr: <single-line>
                    $mExprSingle = [regex]::Match($l, '^\s*expr:\s*(?<v>[^#\r\n]+)')
                    if ($mExprSingle.Success) {
                        $val = $mExprSingle.Groups['v'].Value.Trim()
                        if ($val -and $val -ne '|') {
                            $expr = $val.Trim().Trim('"').Trim("'")
                        }
                        $i++
                        continue
                    }

                    # labels:
                    if ($l -match '^\s*labels:\s*$') {
                        $i++
                        while ($i -lt $lines.Count) {
                            $kv = $lines[$i]
                            if ($kv -match '^\s{10}[A-Za-z0-9_\-]+:\s*') {
                                $parts = $kv.Trim().Split(':', 2)
                                $k = $parts[0].Trim()
                                $v = $parts[1].Trim().Trim('"')
                                $labels[$k] = $v
                                $i++
                                continue
                            }
                            break
                        }
                        continue
                    }

                    # annotations:
                    if ($l -match '^\s*annotations:\s*$') {
                        $i++
                        while ($i -lt $lines.Count) {
                            $al = $lines[$i]

                            # summary: "..."
                            $mSummary = [regex]::Match($al, '^\s{10}summary:\s*(?<v>.+)$')
                            if ($mSummary.Success) {
                                $annotations['summary'] = $mSummary.Groups['v'].Value.Trim().Trim('"')
                                $i++
                                continue
                            }

                            # description: | (multiline)
                            if ($al -match '^\s{10}description:\s*\|\s*$') {
                                $i++
                                $descLines = @()
                                while ($i -lt $lines.Count) {
                                    $dl = $lines[$i]
                                    if ($dl -match '^\s{12}.*') {
                                        $descLines += ($dl -replace '^\s{12}', '')
                                        $i++
                                        continue
                                    }
                                    break
                                }
                                $annotations['description'] = ($descLines -join "`n").TrimEnd()
                                continue
                            }

                            if ($al -match '^\s{6}-\s*alert:' -or $al -match '^\s{2}-\s*name:' -or $al -match '^\s{8}[A-Za-z0-9_\-]+:') {
                                break
                            }

                            $i++
                        }
                        continue
                    }

                    $i++
                }

                $currentGroup.rules += [pscustomobject]@{
                    alertName   = $ruleName
                    expr        = $expr
                    for         = $for
                    labels      = $labels
                    annotations = $annotations
                }
                continue
            }
        }

        $i++
    }

    if ($currentGroup) {
        $groups += $currentGroup
    }

    return $groups
}

function Ensure-GrafanaFolder {
    param(
        [Parameter(Mandatory = $true)][string]$GrafanaEndpoint,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][string]$FolderUid,
        [Parameter(Mandatory = $true)][string]$FolderTitle
    )

    $base = $GrafanaEndpoint.TrimEnd('/')
    $existing = Invoke-RestMethod -Method Get -Uri ("$base/api/folders") -Headers $Headers
    $existingList = @($existing)
    $match = $existingList | Where-Object { $_.uid -eq $FolderUid -or $_.title -eq $FolderTitle } | Select-Object -First 1
    if ($match) {
        return $match.uid
    }

    $body = @{ uid = $FolderUid; title = $FolderTitle } | ConvertTo-Json -Depth 5
    $created = Invoke-RestMethod -Method Post -Uri ("$base/api/folders") -Headers $Headers -Body $body
    if ($created -and $created.uid) {
        return $created.uid
    }

    return $FolderUid
}

function New-GrafanaBasicAuthHeaders {
    param(
        [Parameter(Mandatory = $true)][string]$Username,
        [Parameter(Mandatory = $true)][string]$Password
    )

    $bytes = [System.Text.Encoding]::UTF8.GetBytes("$Username`:$Password")
    $b64 = [Convert]::ToBase64String($bytes)
    return @{
        Authorization  = "Basic $b64"
        "Content-Type" = "application/json"
        Accept         = "application/json"
    }
}

function Wait-GrafanaReady {
    param(
        [Parameter(Mandatory = $true)][string]$GrafanaEndpoint,
        [Parameter(Mandatory = $true)][hashtable]$Headers
    )

    $base = $GrafanaEndpoint.TrimEnd('/')
    $maxAttempts = 30
    for ($i = 1; $i -le $maxAttempts; $i++) {
        try {
            $null = Invoke-RestMethod -Method Get -Uri ("$base/api/health") -Headers $Headers
            return
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    Fail "Grafana at '$GrafanaEndpoint' did not become ready in time."
}

function Ensure-GrafanaDiscordAlerting {
    param(
        [Parameter(Mandatory = $true)][string]$GrafanaEndpoint,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][string]$DiscordWebhookUrl,
        [Parameter(Mandatory = $false)][string]$InstanceLabel = "Grafana"
    )

    if (-not $DiscordWebhookUrl -or $DiscordWebhookUrl.Trim().Length -eq 0) {
        return
    }

    $base = $GrafanaEndpoint.TrimEnd('/')
    $contactPointName = "discord-business-alerts"

    $cpBody = @{
        name                  = $contactPointName
        type                  = "discord"
        settings              = @{ url = $DiscordWebhookUrl }
        disableResolveMessage = $false
    }

    $existing = $null
    try {
        $existing = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/contact-points") -Headers $Headers
    }
    catch {
        $msg = $_.Exception.Message
        if ($_.ErrorDetails.Message) { $msg = $msg + " `n" + $_.ErrorDetails.Message }
        Fail "Failed to query Grafana contact points on '${InstanceLabel}'. Details: $msg"
    }

    $existingList = @()
    try { $existingList = @($existing) } catch { $existingList = @() }
    $existingCp = $existingList | Where-Object { $_.name -eq $contactPointName } | Select-Object -First 1

    $cpJson = ($cpBody | ConvertTo-Json -Depth 20)
    if ($existingCp -and ($existingCp.PSObject.Properties.Name -contains "uid") -and $existingCp.uid) {
        Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/contact-points/$($existingCp.uid)") -Headers $Headers -Body $cpJson | Out-Null
    }
    else {
        try {
            Invoke-RestMethod -Method Post -Uri ("$base/api/v1/provisioning/contact-points") -Headers $Headers -Body $cpJson | Out-Null
        }
        catch {
            $msg = $_.Exception.Message
            if ($_.ErrorDetails.Message) { $msg = $msg + " `n" + $_.ErrorDetails.Message }
            Fail "Failed to create Grafana contact point '${contactPointName}' on '${InstanceLabel}'. Details: $msg"
        }
    }

    # Route alerts to Discord by default (notification policy root receiver).
    try {
        $policy = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/policies") -Headers $Headers
        if ($null -eq $policy) { $policy = @{} }

        if (-not ($policy.PSObject.Properties.Name -contains 'group_by')) {
            $policy | Add-Member -NotePropertyName 'group_by' -NotePropertyValue @('grafana_folder', 'alertname')
        }

        $policy.receiver = $contactPointName
        $policyJson = ($policy | ConvertTo-Json -Depth 50)
        Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/policies") -Headers $Headers -Body $policyJson | Out-Null
    }
    catch {
        $msg = $_.Exception.Message
        if ($_.ErrorDetails.Message) { $msg = $msg + " `n" + $_.ErrorDetails.Message }
        Fail "Failed to update Grafana notification policy on '${InstanceLabel}' to use Discord contact point '${contactPointName}'. Details: $msg"
    }
}

function Import-GrafanaKpiAlertRulesFromMonitoring {
    param(
        [Parameter(Mandatory = $true)][string]$GrafanaEndpoint,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][string]$GrafanaPrometheusDatasourceUid,
        [Parameter(Mandatory = $true)][string]$AlertRulesYmlPath,
        [Parameter(Mandatory = $false)][string]$InstanceLabel = "Grafana"
    )

    $groups = Parse-PrometheusAlertRulesYml -Path $AlertRulesYmlPath
    if (-not $groups -or $groups.Count -eq 0) {
        Fail "No alert rule groups parsed from: $AlertRulesYmlPath"
    }

    $base = $GrafanaEndpoint.TrimEnd('/')
    $folderUid = Ensure-GrafanaFolder -GrafanaEndpoint $base -Headers $Headers -FolderUid "mtogo" -FolderTitle "MToGo"

    $existingRules = @()
    try {
        $existingRules = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $Headers
    }
    catch {
        $existingRules = @()
    }

    $existingList = @($existingRules)

    foreach ($g in $groups) {
        $ruleGroupName = "mtogo-kpi-" + $g.name
        foreach ($r in $g.rules) {
            if (-not $r.expr) { continue }

            $uid = New-StableGrafanaUid -Prefix "mtogo-kpi-" -Name $r.alertName
            $title = $r.alertName

            $labels = @{}
            foreach ($k in $r.labels.Keys) { $labels[$k] = $r.labels[$k] }
            if (-not $labels.ContainsKey('category')) { $labels['category'] = 'business_kpi' }

            $annotations = @{}
            foreach ($k in $r.annotations.Keys) { $annotations[$k] = $r.annotations[$k] }
            if (-not $annotations.ContainsKey('summary')) { $annotations['summary'] = $title }

            $data = @(
                @{
                    refId = 'A'
                    relativeTimeRange = @{ from = 900; to = 0 }
                    datasourceUid = $GrafanaPrometheusDatasourceUid
                    model = @{
                        refId = 'A'
                        expr = $r.expr
                        intervalMs = 1000
                        maxDataPoints = 43200
                    }
                },
                @{
                    refId = 'B'
                    relativeTimeRange = @{ from = 900; to = 0 }
                    datasourceUid = '__expr__'
                    model = @{
                        refId = 'B'
                        type = 'classic_conditions'
                        datasource = @{ type = '__expr__'; uid = '__expr__' }
                        conditions = @(
                            @{
                                type = 'query'
                                query = @{ params = @('A') }
                                reducer = @{ type = 'last'; params = @() }
                                evaluator = @{ type = 'gt'; params = @(0) }
                                operator = @{ type = 'and' }
                            }
                        )
                    }
                }
            )

            $forDuration = '0m'
            if ($r.for -and ("$($r.for)").Trim().Length -gt 0) {
                $forDuration = ("$($r.for)").Trim()
            }

            $rule = @{
                uid         = $uid
                title       = $title
                folderUID   = $folderUid
                ruleGroup   = $ruleGroupName
                condition   = 'B'
                for         = $forDuration
                noDataState  = 'NoData'
                execErrState = 'Error'
                annotations = $annotations
                labels      = $labels
                data        = $data
            }

            $payload = ($rule | ConvertTo-Json -Depth 50)
            $existingRule = $existingList | Where-Object { $_.uid -eq $uid } | Select-Object -First 1
            if ($existingRule) {
                Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/alert-rules/$uid") -Headers $Headers -Body $payload | Out-Null
            }
            else {
                Invoke-RestMethod -Method Post -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $Headers -Body $payload | Out-Null
            }
        }
    }
}

function Remove-GrafanaProvisionedKpiAlertRules {
    param(
        [Parameter(Mandatory = $true)][string]$GrafanaEndpoint,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $false)][string]$InstanceLabel = "Grafana"
    )

    $base = $GrafanaEndpoint.TrimEnd('/')

    $existingRules = @()
    try {
        $existingRules = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $Headers
    }
    catch {
        return
    }

    $rules = @($existingRules)
    $toDelete = @($rules | Where-Object { $_.uid -and ($_.uid -like 'mtogo-kpi-*') })
    if ($toDelete.Count -eq 0) {
        return
    }

    foreach ($r in $toDelete) {
        try {
            Invoke-RestMethod -Method Delete -Uri ("$base/api/v1/provisioning/alert-rules/$($r.uid)") -Headers $Headers | Out-Null
        }
        catch {
            # Best-effort cleanup
        }
    }
}

function Import-AzureManagedGrafanaKpiAlertRulesFromMonitoring {
    param(
        [Parameter(Mandatory = $true)][string]$ResourceGroupName,
        [Parameter(Mandatory = $true)][string]$GrafanaName,
        [Parameter(Mandatory = $true)][string]$GrafanaPrometheusDatasourceUid,
        [Parameter(Mandatory = $true)][string]$AlertRulesYmlPath
    )

    $groups = Parse-PrometheusAlertRulesYml -Path $AlertRulesYmlPath
    if (-not $groups -or $groups.Count -eq 0) {
        Fail "No alert rule groups parsed from: $AlertRulesYmlPath"
    }

    $endpointResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "show",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "--query", "properties.endpoint",
        "-o", "tsv"
    ) -TimeoutSeconds 60
    $grafanaEndpoint = ($endpointResult.StdOut | Out-String).Trim()
    if (-not $grafanaEndpoint) {
        Fail "Failed to determine Grafana endpoint for '${GrafanaName}'."
    }

    $serviceAccountName = "mtogo-provisioner"

    $saListResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "service-account", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "-o", "json"
    ) -TimeoutSeconds 120

    $serviceAccounts = @()
    try { $serviceAccounts = ($saListResult.StdOut | ConvertFrom-Json) } catch { $serviceAccounts = @() }
    $existingSa = $serviceAccounts | Where-Object { $_.name -eq $serviceAccountName } | Select-Object -First 1
    if (-not $existingSa) {
        Invoke-AzCliWithTimeout -Args @(
            "grafana", "service-account", "create",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--service-account", $serviceAccountName,
            "--role", "Admin",
            "-o", "none"
        ) -TimeoutSeconds 180
    }

    $tokenName = "deploy-" + [Guid]::NewGuid().ToString("N")
    $tokenId = $null
    $apiToken = $null

    try {
        $tokenCreateResult = Invoke-AzCliWithTimeout -Args @(
            "grafana", "service-account", "token", "create",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--service-account", $serviceAccountName,
            "--token", $tokenName,
            "--time-to-live", "1h",
            "-o", "json"
        ) -TimeoutSeconds 180

        $tokenObj = $null
        try { $tokenObj = ($tokenCreateResult.StdOut | ConvertFrom-Json) } catch { $tokenObj = $null }
        if ($null -eq $tokenObj) {
            Fail "Failed to parse service account token create response from Azure CLI."
        }

        foreach ($prop in @("key", "token", "secret", "value")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $apiToken = $tokenObj.$prop
                break
            }
        }

        foreach ($prop in @("id", "tokenId", "uid")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $tokenId = $tokenObj.$prop
                break
            }
        }

        if (-not $apiToken) {
            Fail "Azure CLI did not return a usable service account token secret."
        }

        $headers = @{
            Authorization  = "Bearer $apiToken"
            "Content-Type" = "application/json"
            Accept         = "application/json"
        }

        $base = $grafanaEndpoint.TrimEnd('/')
        $folderUid = Ensure-GrafanaFolder -GrafanaEndpoint $base -Headers $headers -FolderUid "mtogo" -FolderTitle "MToGo"
        $existingRules = @()

        try {
            $existingRules = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $headers
        } catch {
            $existingRules = @()
        }

        $existingList = @($existingRules)

        foreach ($g in $groups) {
            $ruleGroupName = "mtogo-kpi-" + $g.name
            foreach ($r in $g.rules) {
                if (-not $r.expr) {
                    continue
                }

                $uid = New-StableGrafanaUid -Prefix "mtogo-kpi-" -Name $r.alertName
                $title = $r.alertName

                $labels = @{}
                foreach ($k in $r.labels.Keys) { $labels[$k] = $r.labels[$k] }
                if (-not $labels.ContainsKey('category')) {
                    $labels['category'] = 'business_kpi'
                }

                $annotations = @{}
                foreach ($k in $r.annotations.Keys) { $annotations[$k] = $r.annotations[$k] }
                if (-not $annotations.ContainsKey('summary')) { $annotations['summary'] = $title }

                # Convert Prometheus alert rule semantics into a Grafana "classic condition":
                # query A runs the original PromQL; condition B checks last(A) > 0.
                $data = @(
                    @{
                        refId = 'A'
                        relativeTimeRange = @{ from = 900; to = 0 }
                        datasourceUid = $GrafanaPrometheusDatasourceUid
                        model = @{
                            refId = 'A'
                            expr = $r.expr
                            intervalMs = 1000
                            maxDataPoints = 43200
                        }
                    },
                    @{
                        refId = 'B'
                        relativeTimeRange = @{ from = 900; to = 0 }
                        datasourceUid = '__expr__'
                        model = @{
                            refId = 'B'
                            type = 'classic_conditions'
                            datasource = @{ type = '__expr__'; uid = '__expr__' }
                            conditions = @(
                                @{
                                    type = 'query'
                                    query = @{ params = @('A') }
                                    reducer = @{ type = 'last'; params = @() }
                                    evaluator = @{ type = 'gt'; params = @(0) }
                                    operator = @{ type = 'and' }
                                }
                            )
                        }
                    }
                )

                $forDuration = '0m'
                if ($r.for -and ("$($r.for)").Trim().Length -gt 0) {
                    $forDuration = ("$($r.for)").Trim()
                }

                $rule = @{
                    uid = $uid
                    title = $title
                    folderUID = $folderUid
                    ruleGroup = $ruleGroupName
                    condition = 'B'
                    for = $forDuration
                    noDataState = 'NoData'
                    execErrState = 'Error'
                    annotations = $annotations
                    labels = $labels
                    data = $data
                }

                $payload = ($rule | ConvertTo-Json -Depth 50)
                $existingRule = $existingList | Where-Object { $_.uid -eq $uid } | Select-Object -First 1
                if ($existingRule) {
                    Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/alert-rules/$uid") -Headers $headers -Body $payload | Out-Null
                }
                else {
                    Invoke-RestMethod -Method Post -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $headers -Body $payload | Out-Null
                }
            }
        }
    }
    finally {
        if ($tokenId) {
            Invoke-AzCliWithTimeout -Args @(
                "grafana", "service-account", "token", "delete",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--service-account", $serviceAccountName,
                "--token", $tokenId,
                "-o", "none"
            ) -TimeoutSeconds 120 -AllowFailure | Out-Null
        }
    }
}

function Remove-AzureManagedGrafanaProvisionedKpiAlertRules {
    param(
        [Parameter(Mandatory = $true)][string]$ResourceGroupName,
        [Parameter(Mandatory = $true)][string]$GrafanaName
    )

    $endpointResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "show",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "--query", "properties.endpoint",
        "-o", "tsv"
    ) -TimeoutSeconds 60 -AllowFailure

    if (-not $endpointResult -or $endpointResult.ExitCode -ne 0) {
        return
    }

    $grafanaEndpoint = ($endpointResult.StdOut | Out-String).Trim()
    if (-not $grafanaEndpoint) {
        return
    }

    $serviceAccountName = "mtogo-provisioner"

    $saListResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "service-account", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "-o", "json"
    ) -TimeoutSeconds 120 -AllowFailure

    if (-not $saListResult -or $saListResult.ExitCode -ne 0) {
        return
    }

    $serviceAccounts = @()
    try { $serviceAccounts = ($saListResult.StdOut | ConvertFrom-Json) } catch { $serviceAccounts = @() }

    $existingSa = $serviceAccounts | Where-Object { $_.name -eq $serviceAccountName } | Select-Object -First 1
    if (-not $existingSa) {
        # No service account => nothing provisioned by our scripts.
        return
    }

    $tokenName = "cleanup-" + [Guid]::NewGuid().ToString("N")
    $tokenId = $null
    $apiToken = $null

    try {
        $tokenCreateResult = Invoke-AzCliWithTimeout -Args @(
            "grafana", "service-account", "token", "create",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--service-account", $serviceAccountName,
            "--token", $tokenName,
            "--time-to-live", "1h",
            "-o", "json"
        ) -TimeoutSeconds 180 -AllowFailure

        if (-not $tokenCreateResult -or $tokenCreateResult.ExitCode -ne 0) {
            return
        }

        $tokenObj = $null
        try { $tokenObj = ($tokenCreateResult.StdOut | ConvertFrom-Json) } catch { $tokenObj = $null }
        if ($null -eq $tokenObj) {
            return
        }

        foreach ($prop in @("key", "token", "secret", "value")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $apiToken = $tokenObj.$prop
                break
            }
        }

        foreach ($prop in @("id", "tokenId", "uid")) {
            if ($tokenObj.PSObject.Properties.Name -contains $prop) {
                $tokenId = $tokenObj.$prop
                break
            }
        }

        if (-not $apiToken) {
            return
        }

        $headers = @{
            Authorization  = "Bearer $apiToken"
            "Content-Type" = "application/json"
            Accept         = "application/json"
        }

        $base = $grafanaEndpoint.TrimEnd('/')
        $existingRules = @()
        try {
            $existingRules = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $headers
        }
        catch {
            return
        }

        $rules = @($existingRules)
        $toDelete = @($rules | Where-Object { $_.uid -and ($_.uid -like 'mtogo-kpi-*') })
        foreach ($r in $toDelete) {
            try {
                Invoke-RestMethod -Method Delete -Uri ("$base/api/v1/provisioning/alert-rules/$($r.uid)") -Headers $headers | Out-Null
            }
            catch {
                # Best-effort cleanup
            }
        }
    }
    finally {
        if ($tokenId) {
            Invoke-AzCliWithTimeout -Args @(
                "grafana", "service-account", "token", "delete",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--service-account", $serviceAccountName,
                "--token", $tokenId,
                "-o", "none"
            ) -TimeoutSeconds 120 -AllowFailure | Out-Null
        }
    }
}

function Get-AzureManagedGrafanaPrometheusDatasourceUid {
    param(
        [Parameter(Mandatory = $true)][string]$ResourceGroupName,
        [Parameter(Mandatory = $true)][string]$GrafanaName,
        [Parameter(Mandatory = $true)][string]$PrometheusQueryEndpoint
    )

    $dsResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "data-source", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "-o", "json"
    ) -TimeoutSeconds 120
    $dsJson = $dsResult.StdOut

    $dataSources = @()
    try { $dataSources = ($dsJson | ConvertFrom-Json) } catch { $dataSources = @() }

    $prometheusDs = $dataSources | Where-Object { $_.type -eq "prometheus" } | Select-Object -First 1
    if (-not $prometheusDs) {
        Write-Host "No Prometheus data source found in '${GrafanaName}'. Creating one..." -ForegroundColor Yellow

        $definitionObj = @{
            name   = "Azure Managed Prometheus"
            type   = "prometheus"
            access = "proxy"
            url    = $PrometheusQueryEndpoint
            jsonData = @{
                httpMethod       = "POST"
                azureCredentials = @{ authType = "msi" }
            }
        }

        $tempRoot = Join-Path $env:TEMP "mtogo-grafana-dashboard-sync"
        if (-not (Test-Path $tempRoot)) {
            New-Item -ItemType Directory -Path $tempRoot | Out-Null
        }

        $dsFile = Join-Path $tempRoot ("datasource-" + $GrafanaName + ".json")
        Write-Utf8FileNoBom -Path $dsFile -Value ($definitionObj | ConvertTo-Json -Depth 20)

        $created = $false
        $maxDsCreateAttempts = 6
        for ($attempt = 1; $attempt -le $maxDsCreateAttempts; $attempt++) {
            if ($attempt -gt 1) {
                Write-Host "Retrying data source create (attempt $attempt/$maxDsCreateAttempts)..." -ForegroundColor Yellow
                Start-Sleep -Seconds (10 * $attempt)
            }

            $createResult = Invoke-AzCliWithTimeout -Args @(
                "grafana", "data-source", "create",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--definition", ("@" + $dsFile),
                "-o", "none"
            ) -TimeoutSeconds 180 -AllowFailure

            if ($createResult.ExitCode -eq 0) {
                $created = $true
                break
            }

            $combinedErr = ((@($createResult.StdErr, $createResult.StdOut) | Where-Object { $_ }) -join "\n")
            if ($combinedErr -match "datasources:create" -or $combinedErr -match "Access denied" -or $combinedErr -match "403") {
                # Most commonly: the logged-in user isn't Grafana Admin on this instance yet, or RBAC hasn't propagated.
                # We'll retry a few times to allow propagation; if it still fails, provide a clear actionable error.
                continue
            }

            break
        }

        if (-not $created) {
            Fail "Failed to create Prometheus data source in '${GrafanaName}'. The logged-in user needs Grafana permissions: datasources:create (typically Grafana Admin on the Azure Managed Grafana instance). If you just applied Terraform, wait ~1-2 minutes for RBAC propagation and rerun deploy."
        }

        $dsResult = Invoke-AzCliWithTimeout -Args @(
            "grafana", "data-source", "list",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "-o", "json"
        ) -TimeoutSeconds 120
        $dsJson = $dsResult.StdOut
        try { $dataSources = ($dsJson | ConvertFrom-Json) } catch { $dataSources = @() }
        $prometheusDs = $dataSources | Where-Object { $_.type -eq "prometheus" } | Select-Object -First 1
    }

    if (-not $prometheusDs) {
        Write-Host "Warning: Still no Prometheus data source found in '${GrafanaName}'. Dashboard import may fail." -ForegroundColor Yellow
        return $null
    }

    if ($prometheusDs.PSObject.Properties.Name -contains "uid") {
        return $prometheusDs.uid
    }

    Write-Host "Warning: Prometheus data source has no uid property; skipping datasource uid rewrite." -ForegroundColor Yellow
    return $null
}

function Get-DashboardUidFromFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    try {
        $obj = (Get-Content -Raw -Path $Path -Encoding UTF8 | ConvertFrom-Json)
        if (-not $obj.uid) {
            return $null
        }
        return $obj.uid
    }
    catch {
        return $null
    }
}

function Import-AzureManagedGrafanaDashboards {
    param(
        [Parameter(Mandatory = $true)][string]$ResourceGroupName,
        [Parameter(Mandatory = $true)][string]$GrafanaName,
        [Parameter(Mandatory = $true)][string]$DashboardDirectory,
        [Parameter(Mandatory = $true)][string]$PrometheusDatasourceUid,
        [switch]$CleanExtra
    )

    if (-not (Test-Path $DashboardDirectory)) {
        Fail "Dashboard directory not found: $DashboardDirectory"
    }

    $dashboardFiles = @(Get-ChildItem -Path $DashboardDirectory -Filter *.json -File | Sort-Object Name)
    if (-not $dashboardFiles -or $dashboardFiles.Count -eq 0) {
        Fail "No dashboard JSON files found in: $DashboardDirectory"
    }

    $allowedUids = New-Object System.Collections.Generic.HashSet[string]
    foreach ($f in $dashboardFiles) {
        $uid = Get-DashboardUidFromFile -Path $f.FullName
        if (-not $uid) {
            Fail "Dashboard JSON missing 'uid': $($f.FullName)"
        }
        $null = $allowedUids.Add($uid)
    }

    $tempRoot = Join-Path $env:TEMP "mtogo-grafana-dashboard-sync"
    if (-not (Test-Path $tempRoot)) {
        New-Item -ItemType Directory -Path $tempRoot | Out-Null
    }

    foreach ($f in $dashboardFiles) {
        $content = Get-Content -Raw -Path $f.FullName -Encoding UTF8
        $importPath = $f.FullName
        $dashboardUid = Get-DashboardUidFromFile -Path $f.FullName

        if ($PrometheusDatasourceUid -and $PrometheusDatasourceUid -ne "prometheus") {
            $content = $content -replace '"uid"\s*:\s*"prometheus"', ('"uid": "' + $PrometheusDatasourceUid + '"')
            $tempFile = Join-Path $tempRoot ("azure-" + $GrafanaName + "-" + $f.Name)
            Write-Utf8FileNoBom -Path $tempFile -Value $content
            $importPath = $tempFile
        }

        # Azure Managed Grafana CLI expects a wrapper payload:
        # { "dashboard": { ... }, "message": "..." }
        $wrappedPath = $null
        try {
            $dashboardObj = ($content | ConvertFrom-Json)
        }
        catch {
            Fail "Failed to parse dashboard JSON: $($f.FullName)"
        }

        $wrappedObj = @{
            dashboard = $dashboardObj
            message   = "Imported by deploy.ps1"
        }

        $wrappedPath = Join-Path $tempRoot ("azurewrap-" + $GrafanaName + "-" + $f.Name)
        Write-Utf8FileNoBom -Path $wrappedPath -Value ($wrappedObj | ConvertTo-Json -Depth 100)

        Write-Host "Importing dashboard: $($f.Name) -> ${GrafanaName}" -ForegroundColor Cyan
        $importStart = Get-Date
        try {
            $importResult = Invoke-AzCliWithTimeout -Args @(
                "grafana", "dashboard", "create",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--definition", ("@" + $wrappedPath),
                "--overwrite", "true",
                "-o", "none"
            ) -TimeoutSeconds 900

            Write-Host ("Imported in {0}s" -f [math]::Round($importResult.Seconds, 1)) -ForegroundColor DarkGray
        }
        catch {
            $msg = $_.Exception.Message
            $elapsed = (New-TimeSpan -Start $importStart -End (Get-Date)).TotalSeconds

            # If import timed out or otherwise failed, check whether the dashboard actually exists now.
            if ($dashboardUid) {
                Write-Host ("Import did not return cleanly after {0}s; checking whether dashboard uid '{1}' exists..." -f [math]::Round($elapsed, 1), $dashboardUid) -ForegroundColor Yellow
                $listResult = Invoke-AzCliWithTimeout -Args @(
                    "grafana", "dashboard", "list",
                    "-g", $ResourceGroupName,
                    "-n", $GrafanaName,
                    "--query", "[].{uid:uid,title:title}",
                    "-o", "json"
                ) -TimeoutSeconds 120 -AllowFailure

                if ($listResult.ExitCode -eq 0 -and $listResult.StdOut) {
                    try {
                        $existing = ($listResult.StdOut | ConvertFrom-Json)
                        $found = $existing | Where-Object { $_.uid -eq $dashboardUid } | Select-Object -First 1
                        if ($found) {
                            Write-Host "Dashboard appears to have been imported successfully." -ForegroundColor Green
                            continue
                        }
                    }
                    catch {
                        # ignore parse errors
                    }
                }
            }

            if ($msg) {
                Fail $msg
            }
            throw
        }
    }

    $dashResult = Invoke-AzCliWithTimeout -Args @(
        "grafana", "dashboard", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "--query", "[].{uid:uid,title:title,folderUid:folderUid}",
        "-o", "json"
    ) -TimeoutSeconds 120
    $dashJson = $dashResult.StdOut
    $dashboards = @()
    try { $dashboards = ($dashJson | ConvertFrom-Json) } catch { $dashboards = @() }

    if ($CleanExtra) {
        # Many Azure Managed Grafana "default" dashboards are provisioned/read-only and cannot be
        # deleted individually. They do, however, live in folders which can be deleted. We delete
        # any folder that contains non-repo dashboards (and contains no repo dashboards).

        $foldersToDelete = New-Object System.Collections.Generic.HashSet[string]
        foreach ($d in $dashboards) {
            if (-not $d.uid) { continue }
            if ($allowedUids.Contains([string]$d.uid)) { continue }
            if ($d.folderUid) {
                $null = $foldersToDelete.Add([string]$d.folderUid)
            }
        }

        foreach ($folderUid in ($foldersToDelete | Sort-Object)) {
            Write-Host "Deleting non-repo dashboard folder from ${GrafanaName}: ${folderUid}" -ForegroundColor Yellow
            $folderDel = Invoke-AzCliWithTimeout -Args @(
                "grafana", "folder", "delete",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--folder", $folderUid,
                "-o", "none"
            ) -TimeoutSeconds 180 -AllowFailure
            if ($folderDel.ExitCode -ne 0) {
                Write-Host "Warning: Failed to delete folder uid '${folderUid}' from '${GrafanaName}'." -ForegroundColor Yellow
                if ($folderDel.StdErr) { Write-Host ($folderDel.StdErr.Trim()) -ForegroundColor DarkYellow }
            }
        }

        # Re-list and then delete any remaining non-repo dashboards (e.g., those in General / no folder).
        $dashAfterFolder = Invoke-AzCliWithTimeout -Args @(
            "grafana", "dashboard", "list",
            "-g", $ResourceGroupName,
            "-n", $GrafanaName,
            "--query", "[].{uid:uid,title:title,folderUid:folderUid}",
            "-o", "json"
        ) -TimeoutSeconds 120 -AllowFailure

        if ($dashAfterFolder.ExitCode -eq 0 -and $dashAfterFolder.StdOut) {
            try { $dashboards = ($dashAfterFolder.StdOut | ConvertFrom-Json) } catch { }
        }

        foreach ($d in $dashboards) {
            if (-not $d.uid) { continue }
            if ($allowedUids.Contains([string]$d.uid)) { continue }

            Write-Host "Deleting non-repo dashboard from ${GrafanaName}: $($d.title) ($($d.uid))" -ForegroundColor Yellow
            $delResult = Invoke-AzCliWithTimeout -Args @(
                "grafana", "dashboard", "delete",
                "-g", $ResourceGroupName,
                "-n", $GrafanaName,
                "--dashboard", [string]$d.uid,
                "-o", "none"
            ) -TimeoutSeconds 120 -AllowFailure

            if ($delResult.ExitCode -ne 0) {
                Write-Host "Warning: Failed to delete dashboard uid '$($d.uid)' from '${GrafanaName}'." -ForegroundColor Yellow
                if ($delResult.StdErr) { Write-Host ($delResult.StdErr.Trim()) -ForegroundColor DarkYellow }
            }
        }
    }

    $dashResult2 = Invoke-AzCliWithTimeout -Args @(
        "grafana", "dashboard", "list",
        "-g", $ResourceGroupName,
        "-n", $GrafanaName,
        "--query", "[].{uid:uid,title:title,folderUid:folderUid}",
        "-o", "json"
    ) -TimeoutSeconds 120 -AllowFailure
    if ($dashResult2.ExitCode -eq 0 -and $dashResult2.StdOut) {
        try { $dashboards = ($dashResult2.StdOut | ConvertFrom-Json) } catch { }
    }

    Write-Host "Dashboards currently in ${GrafanaName}:" -ForegroundColor Cyan
    foreach ($d in ($dashboards | Sort-Object title)) {
        if ($d.title -and $d.uid) {
            Write-Host ("  - {0} ({1})" -f $d.title, $d.uid) -ForegroundColor White
        }
    }

    if ($CleanExtra) {
        $remainingNonRepo = @($dashboards | Where-Object { $_.uid -and -not $allowedUids.Contains([string]$_.uid) })
        if ($remainingNonRepo.Count -gt 0) {
            $names = ($remainingNonRepo | Sort-Object title | ForEach-Object { "{0} ({1})" -f $_.title, $_.uid }) -join "; "
            Fail "Non-repo dashboards still present in '${GrafanaName}' after cleanup: ${names}"
        }
    }
}

# Allow dot-sourcing this script to reuse helper functions (e.g., for local smoke tests)
# without executing the deployment logic.
if ($MyInvocation.InvocationName -eq '.') {
    return
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

if ($Context -eq "azure") {
    Ensure-AzureSubscription

    # Terraform can fail with 409 MissingSubscriptionRegistration unless these are registered.
    Ensure-AzureResourceProviders -Namespaces @(
        "Microsoft.ContainerService",
        "Microsoft.Monitor",
        "Microsoft.Dashboard",
        "Microsoft.Network",
        "Microsoft.DBforPostgreSQL",
        "Microsoft.OperationalInsights"
    )
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

# Pass optional demo seeding toggle to Terraform via TF_VAR_*.
$env:TF_VAR_seed_demo_data = $(if ($SeedDemoData) { "true" } else { "false" })

# Local-only: if demo seeding is requested, ensure the seed Job will re-run.
# (Jobs are one-shot; if the Job already exists/completed from a previous run, Terraform will not re-execute it.)
if ($Context -eq "local" -and $SeedDemoData) {
    Write-Host "`nCleaning up any previous demo seed job (so it re-runs)..." -ForegroundColor Yellow
    try {
        kubectl delete job -n mtogo -l "app.kubernetes.io/name=mtogo-demo-seed" --ignore-not-found=true 1>$null 2>$null
    }
    catch {
        # Best-effort only (namespace may not exist yet)
    }
}

# Azure-only: if demo seeding is requested, try to clean up any previous seed Job BEFORE apply.
# This ensures the Job will re-run on repeated deployments.
if ($Context -eq "azure" -and $SeedDemoData) {
    if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
        Write-Host "Error: kubectl is not installed" -ForegroundColor Red
        Fail "kubectl is not installed"
    }

    Write-Host "`nAttempting to clean up any previous demo seed job on AKS (so it re-runs)..." -ForegroundColor Yellow

    # Best-effort: if outputs/state exist, we can fetch AKS credentials and delete the job.
    $preOutputs = $null
    try {
        $preOutputs = Get-TerraformOutputs -WorkingDirectory $tfDir
    }
    catch {
        $preOutputs = $null
    }

    $rgPre = $null
    $aksPre = $null
    try {
        if ($preOutputs -and $preOutputs.resource_group_name) { $rgPre = $preOutputs.resource_group_name.value }
        if ($preOutputs -and $preOutputs.aks_cluster_name) { $aksPre = $preOutputs.aks_cluster_name.value }
    }
    catch {
        $rgPre = $null
        $aksPre = $null
    }

    if ($rgPre -and $aksPre) {
        try {
            Invoke-AzCliWithTimeout -Args @(
                "aks", "get-credentials",
                "-g", $rgPre,
                "-n", $aksPre,
                "--overwrite-existing"
            ) -TimeoutSeconds 300 | Out-Null
        }
        catch {
            Write-Host "Warning: Failed to fetch AKS credentials for pre-apply seed cleanup. Demo seeding may not re-run." -ForegroundColor Yellow
        }

        try {
            kubectl delete job -n mtogo -l "app.kubernetes.io/name=mtogo-demo-seed" --ignore-not-found=true 1>$null 2>$null
        }
        catch {
            Write-Host "Warning: Failed to delete existing demo seed Job before apply. Demo seeding may not re-run." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "(Skipping pre-apply seed Job cleanup: Terraform outputs not available yet.)" -ForegroundColor DarkGray
    }
}

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

    if ($SeedDemoData) {
        Write-Host "`nWaiting for demo seed job to complete..." -ForegroundColor Yellow

        $seedJobName = ""
        try {
            $seedJobName = (kubectl get jobs -n mtogo -l "app.kubernetes.io/name=mtogo-demo-seed" -o jsonpath="{.items[0].metadata.name}" 2>$null).Trim()
        }
        catch {
            $seedJobName = ""
        }

        if (-not $seedJobName) {
            Write-Host "SeedDemoData was requested, but no demo seed Job was found in namespace 'mtogo'." -ForegroundColor Red
            Write-Host "Hint: kubectl get jobs -n mtogo -l app.kubernetes.io/name=mtogo-demo-seed" -ForegroundColor Yellow
            Fail "Demo seed Job was not found"
        }

        $waitSucceeded = $true
        try {
            kubectl wait --for=condition=complete --timeout=900s job/$seedJobName -n mtogo
        }
        catch {
            $waitSucceeded = $false
        }

        if (-not $waitSucceeded) {
            Write-Host "`nDemo seed Job did not complete successfully. Showing diagnostics..." -ForegroundColor Red
            try { kubectl describe job $seedJobName -n mtogo | Out-Host } catch { }
            Write-Host "`nDemo seed Job logs:" -ForegroundColor Yellow
            try { kubectl logs job/$seedJobName -n mtogo --all-containers=true --tail=-1 | Out-Host } catch { }
            Fail "Demo seed Job failed"
        }
    }

    Write-Host "`n================================================" -ForegroundColor Green
    Write-Host "  Deployment Complete!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green

    Write-Host "`nPod Status:" -ForegroundColor Cyan
    kubectl get pods -n mtogo

    $outputs = Get-TerraformOutputs -WorkingDirectory $tfDir

    if ($outputs.endpoints) {
        Write-EndpointSection -Title "Access (Local Kubernetes via ingress)" -Map $outputs.endpoints.value
    }
    else {
        Write-Host "`nAccess" -ForegroundColor Cyan
        Write-Host "  (No 'endpoints' output found; run: terraform output)" -ForegroundColor Yellow
    }

    if ($ProvisionLocalGrafanaAlerting) {
        # Local KPI Grafana: OPTIONAL import of KPI alert rules into Grafana-managed alerting.
        # Default is off because it can generate noisy 'DatasourceNoData' alerts.
        try {
            $grafanaEndpoint = $null
            if ($outputs.endpoints -and $outputs.endpoints.value -and $outputs.endpoints.value.grafana_kpi) {
                $grafanaEndpoint = $outputs.endpoints.value.grafana_kpi
            }
            if (-not $grafanaEndpoint) { $grafanaEndpoint = "http://localhost:3000" }

            $grafanaUser = Get-TerraformTfvarsStringValue -TerraformDirectory $tfDir -VariableName "grafana_kpi_admin_username"
            $grafanaPass = Get-TerraformTfvarsStringValue -TerraformDirectory $tfDir -VariableName "grafana_kpi_admin_password"

            if (-not $grafanaUser) { $grafanaUser = "admin" }
            if (-not $grafanaPass) { $grafanaPass = "admin" }

            $headers = New-GrafanaBasicAuthHeaders -Username $grafanaUser -Password $grafanaPass
            Wait-GrafanaReady -GrafanaEndpoint $grafanaEndpoint -Headers $headers

            $discordWebhookUrl = Get-TerraformTfvarsStringValue -TerraformDirectory $tfDir -VariableName "discord_webhook_url"
            if ($discordWebhookUrl -and $discordWebhookUrl.Trim().Length -gt 0) {
                Ensure-GrafanaDiscordAlerting -GrafanaEndpoint $grafanaEndpoint -Headers $headers -DiscordWebhookUrl $discordWebhookUrl -InstanceLabel "Local KPI Grafana"
            }

            $kpiAlertRulesYml = Join-Path $RootDir "monitoring\prometheus\alert_rules.yml"
            Import-GrafanaKpiAlertRulesFromMonitoring -GrafanaEndpoint $grafanaEndpoint -Headers $headers -GrafanaPrometheusDatasourceUid "prometheus" -AlertRulesYmlPath $kpiAlertRulesYml -InstanceLabel "Local KPI Grafana"
        }
        catch {
            Write-Host "Warning: Failed to provision local KPI Grafana alert rules. Details: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "`nSkipping Grafana-managed alerting provisioning for local deployment (using Prometheus + Alertmanager for alerts)." -ForegroundColor DarkGray
        Write-Host "To enable Grafana-managed alerting anyway, re-run with: -ProvisionLocalGrafanaAlerting" -ForegroundColor DarkGray

        # Best-effort cleanup: remove previously provisioned KPI rules that this script created.
        # This prevents old Grafana rules (and DatasourceNoData notifications) from continuing to spam Discord.
        try {
            $grafanaEndpoint = $null
            if ($outputs.endpoints -and $outputs.endpoints.value -and $outputs.endpoints.value.grafana_kpi) {
                $grafanaEndpoint = $outputs.endpoints.value.grafana_kpi
            }
            if (-not $grafanaEndpoint) { $grafanaEndpoint = "http://localhost:3000" }

            $grafanaUser = Get-TerraformTfvarsStringValue -TerraformDirectory $tfDir -VariableName "grafana_kpi_admin_username"
            $grafanaPass = Get-TerraformTfvarsStringValue -TerraformDirectory $tfDir -VariableName "grafana_kpi_admin_password"
            if (-not $grafanaUser) { $grafanaUser = "admin" }
            if (-not $grafanaPass) { $grafanaPass = "admin" }

            $headers = New-GrafanaBasicAuthHeaders -Username $grafanaUser -Password $grafanaPass
            Wait-GrafanaReady -GrafanaEndpoint $grafanaEndpoint -Headers $headers
            Remove-GrafanaProvisionedKpiAlertRules -GrafanaEndpoint $grafanaEndpoint -Headers $headers -InstanceLabel "Local KPI Grafana"
        }
        catch {
            # Best-effort only
        }
    }
}

if ($Context -eq "azure") {
    $outputs = Get-TerraformOutputs -WorkingDirectory $tfDir

    if ($SeedDemoData) {
        if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
            Write-Host "Error: kubectl is not installed" -ForegroundColor Red
            Fail "kubectl is not installed"
        }

        $rg = $outputs.resource_group_name.value
        $aksName = $outputs.aks_cluster_name.value
        if (-not $rg -or -not $aksName) {
            Fail "SeedDemoData requires Terraform outputs 'resource_group_name' and 'aks_cluster_name'."
        }

        Write-Host "`nFetching AKS credentials..." -ForegroundColor Yellow
        Invoke-AzCliWithTimeout -Args @(
            "aks", "get-credentials",
            "-g", $rg,
            "-n", $aksName,
            "--overwrite-existing"
        ) -TimeoutSeconds 300 | Out-Null

        Write-Host "`nWaiting for demo seed job to complete..." -ForegroundColor Yellow
        $seedJobName = ""
        try {
            $seedJobName = (kubectl get jobs -n mtogo -l "app.kubernetes.io/name=mtogo-demo-seed" -o jsonpath="{.items[0].metadata.name}" 2>$null).Trim()
        }
        catch {
            $seedJobName = ""
        }

        if (-not $seedJobName) {
            Write-Host "SeedDemoData was requested, but no demo seed Job was found in namespace 'mtogo'." -ForegroundColor Red
            Write-Host "Hint: kubectl get jobs -n mtogo -l app.kubernetes.io/name=mtogo-demo-seed" -ForegroundColor Yellow
            Fail "Demo seed Job was not found"
        }

        $waitSucceeded = $true
        try {
            kubectl wait --for=condition=complete --timeout=900s job/$seedJobName -n mtogo
        }
        catch {
            $waitSucceeded = $false
        }

        if (-not $waitSucceeded) {
            Write-Host "`nDemo seed Job did not complete successfully. Showing diagnostics..." -ForegroundColor Red
            try { kubectl describe job $seedJobName -n mtogo | Out-Host } catch { }
            Write-Host "`nDemo seed Job logs:" -ForegroundColor Yellow
            try { kubectl logs job/$seedJobName -n mtogo --all-containers=true --tail=-1 | Out-Host } catch { }
            Fail "Demo seed Job failed"
        }
    }

    # Mandatory: ensure Azure instances contain ONLY repo dashboards.
    Ensure-AzureCliExtension -Name "amg"

    $rg = $outputs.resource_group_name.value
    $kpiGrafanaName = $outputs.kpi_grafana_name.value
    $sloGrafanaName = $outputs.slo_grafana_name.value
    $promEndpoint = $outputs.prometheus_query_endpoint.value

    if (-not $rg -or -not $kpiGrafanaName -or -not $sloGrafanaName -or -not $promEndpoint) {
        Fail "Missing required Terraform outputs for dashboard sync (resource_group_name, kpi_grafana_name, slo_grafana_name, prometheus_query_endpoint). Re-run terraform apply to refresh outputs."
    }

    Write-Host "`nSyncing Azure Managed Grafana dashboards from repo (and removing all non-repo dashboards)..." -ForegroundColor Yellow

    Invoke-AzCliWithTimeout -Args @(
        "grafana", "wait",
        "-g", $rg,
        "-n", $kpiGrafanaName,
        "--created",
        "--timeout", "900",
        "-o", "none"
    ) -TimeoutSeconds 930

    Invoke-AzCliWithTimeout -Args @(
        "grafana", "wait",
        "-g", $rg,
        "-n", $sloGrafanaName,
        "--created",
        "--timeout", "900",
        "-o", "none"
    ) -TimeoutSeconds 930

    $kpiDsUid = Get-AzureManagedGrafanaPrometheusDatasourceUid -ResourceGroupName $rg -GrafanaName $kpiGrafanaName -PrometheusQueryEndpoint $promEndpoint
    $sloDsUid = Get-AzureManagedGrafanaPrometheusDatasourceUid -ResourceGroupName $rg -GrafanaName $sloGrafanaName -PrometheusQueryEndpoint $promEndpoint

    if (-not $kpiDsUid) { $kpiDsUid = "prometheus" }
    if (-not $sloDsUid) { $sloDsUid = "prometheus" }

    if ($ProvisionAzureGrafanaAlerting) {
        # OPTIONAL: KPI alerting -> Discord + KPI alert rules (Grafana-managed alerting).
        # Default is off to avoid Grafana 'DatasourceNoData' alerts and to ensure Discord only receives
        # Alertmanager template embeds.
        $discordWebhookUrl = Get-TerraformTfvarsStringValue -TerraformDirectory $tfDir -VariableName "discord_webhook_url"
        Ensure-AzureManagedGrafanaKpiDiscordAlerting -ResourceGroupName $rg -GrafanaName $kpiGrafanaName -DiscordWebhookUrl $discordWebhookUrl

        $kpiAlertRulesYml = Join-Path $RootDir "monitoring\prometheus\alert_rules.yml"
        Import-AzureManagedGrafanaKpiAlertRulesFromMonitoring -ResourceGroupName $rg -GrafanaName $kpiGrafanaName -GrafanaPrometheusDatasourceUid $kpiDsUid -AlertRulesYmlPath $kpiAlertRulesYml
    }
    else {
        Write-Host "`nSkipping Azure Managed Grafana alerting provisioning (using Prometheus + Alertmanager for alerts)." -ForegroundColor DarkGray
        Write-Host "To enable Azure Grafana-managed alerting anyway, re-run with: -ProvisionAzureGrafanaAlerting" -ForegroundColor DarkGray

        # Best-effort cleanup: remove previously provisioned KPI rules that this script created.
        # This prevents old Grafana rules (and DatasourceNoData notifications) from continuing to spam Discord.
        try {
            Remove-AzureManagedGrafanaKpiDiscordAlerting -ResourceGroupName $rg -GrafanaName $kpiGrafanaName
            Remove-AzureManagedGrafanaProvisionedKpiAlertRules -ResourceGroupName $rg -GrafanaName $kpiGrafanaName
        }
        catch {
            # Best-effort only
        }
    }

    $kpiDashDir = Join-Path $RootDir "monitoring\grafana\dashboards"

    $sloDashDir = Join-Path $RootDir "monitoring-infrastructure\grafana\dashboards"

    Import-AzureManagedGrafanaDashboards -ResourceGroupName $rg -GrafanaName $kpiGrafanaName -DashboardDirectory $kpiDashDir -PrometheusDatasourceUid $kpiDsUid -CleanExtra
    Import-AzureManagedGrafanaDashboards -ResourceGroupName $rg -GrafanaName $sloGrafanaName -DashboardDirectory $sloDashDir -PrometheusDatasourceUid $sloDsUid -CleanExtra

    Write-Host "`n================================================" -ForegroundColor Green
    Write-Host "  Deployment Complete!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green

    # Prefer structured endpoints if present, otherwise print a few common ones.
    if ($outputs.endpoints) {
        Write-EndpointSection -Title "Access" -Map $outputs.endpoints.value
    }
    else {
        Write-Host "`nAccess" -ForegroundColor Cyan
        foreach ($name in @('website_url','api_url','legacy_api_url','kpi_grafana_endpoint','slo_grafana_endpoint','prometheus_query_endpoint')) {
            if ($outputs.$name) {
                Write-Host ("  {0}: {1}" -f $name, $outputs.$name.value) -ForegroundColor White
            }
        }
    }
}
