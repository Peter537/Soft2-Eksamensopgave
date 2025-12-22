# Provision Azure Managed Grafana (KPI + SLO) after Terraform apply.
#
# This script is intended for CI/CD workflows (production).
# It enforces strict dashboard separation:
# - KPI instance: ONLY dashboards from /monitoring
# - SLO instance: ONLY dashboards from /monitoring-infrastructure
#
# It also provisions KPI alerting:
# - Discord contact point (requires DISCORD_WEBHOOK_URL)
# - Imports Grafana-managed alert rules from /monitoring/prometheus/alert_rules.yml

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ResourceGroupName,
    [Parameter(Mandatory = $true)][string]$KpiGrafanaName,
    [Parameter(Mandatory = $true)][string]$SloGrafanaName,
    [Parameter(Mandatory = $true)][string]$PrometheusQueryEndpoint,
    [Parameter(Mandatory = $false)][string]$DiscordWebhookUrl = $env:DISCORD_WEBHOOK_URL,
    [Parameter(Mandatory = $false)][string]$RepoRoot = $(Resolve-Path (Join-Path $PSScriptRoot "..\.."))
)

$ErrorActionPreference = 'Stop'

try {
    $OutputEncoding = [System.Text.Encoding]::UTF8
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
} catch {
}

function Fail([string]$Message) {
    throw $Message
}

function Ensure-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Fail "Required command not found on PATH: ${Name}"
    }
}

function Invoke-AzCli([string[]]$Args, [switch]$AllowFailure) {
    Ensure-Command -Name 'az'

    $cmd = 'az ' + ($Args | ForEach-Object {
            if ($_ -match '[\s&()^%=!"'']') {
                '"' + ($_ -replace '"', '\\"') + '"'
            }
            else {
                $_
            }
        } | Out-String).Trim()

    Write-Host "Running: $cmd" -ForegroundColor DarkGray

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = 'az'
    $pinfo.RedirectStandardError = $true
    $pinfo.RedirectStandardOutput = $true
    $pinfo.UseShellExecute = $false

    foreach ($a in $Args) {
        $null = $pinfo.ArgumentList.Add($a)
    }

    $p = New-Object System.Diagnostics.Process
    $p.StartInfo = $pinfo
    $null = $p.Start()

    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $p.WaitForExit()

    if ($p.ExitCode -ne 0 -and -not $AllowFailure) {
        $combined = ((@($stderr, $stdout) | Where-Object { $_ -and $_.Trim().Length -gt 0 }) -join "\n").Trim()
        if (-not $combined) { $combined = "Azure CLI failed with exit code $($p.ExitCode)" }
        Fail $combined
    }

    return [pscustomobject]@{
        ExitCode = $p.ExitCode
        StdOut   = $stdout
        StdErr   = $stderr
    }
}

function Ensure-AzExtension([string]$Name) {
    $ext = Invoke-AzCli -Args @('extension', 'show', '--name', $Name, '-o', 'json') -AllowFailure
    if ($ext.ExitCode -ne 0) {
        Invoke-AzCli -Args @('extension', 'add', '--name', $Name, '--upgrade', '--yes', '-o', 'none') | Out-Null
        return
    }

    Invoke-AzCli -Args @('extension', 'update', '--name', $Name, '-o', 'none') -AllowFailure | Out-Null
}

function Write-Utf8NoBom([string]$Path, [string]$Value) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Value, $utf8NoBom)
}

function Get-GrafanaEndpoint([string]$GrafanaName) {
    $r = Invoke-AzCli -Args @('grafana', 'show', '-g', $ResourceGroupName, '-n', $GrafanaName, '--query', 'properties.endpoint', '-o', 'tsv')
    $ep = ($r.StdOut | Out-String).Trim()
    if (-not $ep) { Fail "Failed to determine Grafana endpoint for '${GrafanaName}'." }
    return $ep.TrimEnd('/')
}

function Ensure-ServiceAccountAdmin([string]$GrafanaName, [string]$ServiceAccountName) {
    $list = Invoke-AzCli -Args @('grafana', 'service-account', 'list', '-g', $ResourceGroupName, '-n', $GrafanaName, '-o', 'json')
    $sas = @()
    try { $sas = ($list.StdOut | ConvertFrom-Json) } catch { $sas = @() }

    $match = $sas | Where-Object { $_.name -eq $ServiceAccountName } | Select-Object -First 1
    if (-not $match) {
        Invoke-AzCli -Args @('grafana', 'service-account', 'create', '-g', $ResourceGroupName, '-n', $GrafanaName, '--service-account', $ServiceAccountName, '--role', 'Admin', '-o', 'none') | Out-Null
    }
}

function New-GrafanaToken([string]$GrafanaName, [string]$ServiceAccountName) {
    $tokenName = 'ci-' + [Guid]::NewGuid().ToString('N')
    $create = Invoke-AzCli -Args @('grafana', 'service-account', 'token', 'create', '-g', $ResourceGroupName, '-n', $GrafanaName, '--service-account', $ServiceAccountName, '--token', $tokenName, '--time-to-live', '1h', '-o', 'json')
    $stdout = ($create.StdOut | Out-String)
    $stderr = ($create.StdErr | Out-String)
    $stdoutTrimmed = ($stdout ?? '').Trim()

    $obj = $null
    try {
        if ($stdoutTrimmed) {
            $obj = ($stdoutTrimmed | ConvertFrom-Json)
        }
    } catch {
        $obj = $null
    }

    # Azure CLI / amg extension sometimes emits non-JSON progress lines before the JSON payload.
    if ($null -eq $obj -and $stdoutTrimmed) {
        $idxObj = $stdoutTrimmed.LastIndexOf('{')
        if ($idxObj -ge 0) {
            $candidate = $stdoutTrimmed.Substring($idxObj)
            try { $obj = ($candidate | ConvertFrom-Json) } catch { $obj = $null }
        }
    }

    $token = $null
    $tokenId = $null

    if ($null -ne $obj) {
        foreach ($p in @('key', 'token', 'secret', 'value')) {
            if ($obj.PSObject.Properties.Name -contains $p) { $token = $obj.$p; break }
        }
        foreach ($p in @('id', 'tokenId', 'uid')) {
            if ($obj.PSObject.Properties.Name -contains $p) { $tokenId = $obj.$p; break }
        }
    }

    # Final fallback: extract token directly from raw output (redacts in errors).
    if (-not $token -and $stdoutTrimmed) {
        $m = [regex]::Matches($stdoutTrimmed, '"(key|token|secret|value)"\s*:\s*"([^"]+)"')
        if ($m.Count -gt 0) {
            $token = $m[$m.Count - 1].Groups[2].Value
        }

        $mid = [regex]::Matches($stdoutTrimmed, '"(id|tokenId|uid)"\s*:\s*"([^"]+)"')
        if ($mid.Count -gt 0) {
            $tokenId = $mid[$mid.Count - 1].Groups[2].Value
        }
    }

    if (-not $token) {
        $safe = $stdoutTrimmed
        if ($safe) {
            $safe = [regex]::Replace($safe, '"(key|token|secret|value)"\s*:\s*"[^"]+"', '"$1":"***REDACTED***"')
        }
        $details = (@(
                if ($stderr) { "STDERR: $($stderr.Trim())" }
                if ($safe) { "STDOUT (redacted): $safe" }
            ) | Where-Object { $_ -and $_.Trim().Length -gt 0 }) -join "\n"
        if (-not $details) { $details = 'No output captured from Azure CLI.' }
        Fail ("Azure CLI did not return a usable service account token secret.\n" + $details)
    }

    return [pscustomobject]@{ Token = $token; TokenId = $tokenId }
}

function Remove-GrafanaToken([string]$GrafanaName, [string]$ServiceAccountName, $TokenId) {
    if (-not $TokenId) { return }
    Invoke-AzCli -Args @('grafana', 'service-account', 'token', 'delete', '-g', $ResourceGroupName, '-n', $GrafanaName, '--service-account', $ServiceAccountName, '--token', [string]$TokenId, '-o', 'none') -AllowFailure | Out-Null
}

function Get-OrCreate-PrometheusDatasourceUid([string]$GrafanaName) {
    $list = Invoke-AzCli -Args @('grafana', 'data-source', 'list', '-g', $ResourceGroupName, '-n', $GrafanaName, '-o', 'json')
    $dss = @()
    try { $dss = ($list.StdOut | ConvertFrom-Json) } catch { $dss = @() }

    $prom = $dss | Where-Object { $_.type -eq 'prometheus' } | Select-Object -First 1
    if (-not $prom) {
        Write-Host "No Prometheus datasource found in '${GrafanaName}'. Creating one..." -ForegroundColor Yellow

        $def = @{
            name   = 'Azure Managed Prometheus'
            type   = 'prometheus'
            access = 'proxy'
            url    = $PrometheusQueryEndpoint
            jsonData = @{
                httpMethod       = 'POST'
                azureCredentials = @{ authType = 'msi' }
            }
        }

        $tmp = Join-Path ([System.IO.Path]::GetTempPath()) 'mtogo-amg-ci'
        if (-not (Test-Path $tmp)) { New-Item -ItemType Directory -Path $tmp | Out-Null }
        $path = Join-Path $tmp ("datasource-${GrafanaName}.json")
        Write-Utf8NoBom -Path $path -Value ($def | ConvertTo-Json -Depth 20)

        Invoke-AzCli -Args @('grafana', 'data-source', 'create', '-g', $ResourceGroupName, '-n', $GrafanaName, '--definition', ('@' + $path), '-o', 'none') | Out-Null

        $list = Invoke-AzCli -Args @('grafana', 'data-source', 'list', '-g', $ResourceGroupName, '-n', $GrafanaName, '-o', 'json')
        try { $dss = ($list.StdOut | ConvertFrom-Json) } catch { $dss = @() }
        $prom = $dss | Where-Object { $_.type -eq 'prometheus' } | Select-Object -First 1
    }

    if (-not $prom) { return $null }
    if ($prom.PSObject.Properties.Name -contains 'uid') { return $prom.uid }
    return $null
}

function Get-DashboardUidFromFile([string]$Path) {
    try {
        $obj = (Get-Content -Raw -Path $Path -Encoding UTF8 | ConvertFrom-Json)
        return $obj.uid
    } catch {
        return $null
    }
}

function Import-DashboardsStrict {
    param(
        [Parameter(Mandatory = $true)][string]$GrafanaName,
        [Parameter(Mandatory = $true)][string]$DashboardDirectory,
        [Parameter(Mandatory = $true)][string]$DatasourceUid,
        [string[]]$AllowedFolderTitles = @('General', 'MToGo')
    )
    if (-not (Test-Path $DashboardDirectory)) {
        Fail "Dashboard directory not found: $DashboardDirectory"
    }

    $files = @(Get-ChildItem -Path $DashboardDirectory -Filter *.json -File | Sort-Object Name)
    if (-not $files -or $files.Count -eq 0) {
        Fail "No dashboard JSON files found in: $DashboardDirectory"
    }

    $allowedUids = New-Object System.Collections.Generic.HashSet[string]
    foreach ($f in $files) {
        $uid = Get-DashboardUidFromFile -Path $f.FullName
        if (-not $uid) { Fail "Dashboard JSON missing 'uid': $($f.FullName)" }
        $null = $allowedUids.Add([string]$uid)
    }

    # First: delete any non-repo folders (this is the only reliable way to remove some provisioned/default dashboards).
    $foldersResult = Invoke-AzCli -Args @('grafana', 'folder', 'list', '-g', $ResourceGroupName, '-n', $GrafanaName, '-o', 'json') -AllowFailure
    if ($foldersResult.ExitCode -eq 0 -and $foldersResult.StdOut) {
        $folders = @()
        try { $folders = ($foldersResult.StdOut | ConvertFrom-Json) } catch { $folders = @() }
        foreach ($folder in $folders) {
            $title = [string]$folder.title
            $uid = [string]$folder.uid
            if (-not $uid) { continue }
            if ($AllowedFolderTitles -contains $title) { continue }

            Write-Host "Deleting non-repo folder from ${GrafanaName}: ${title} (${uid})" -ForegroundColor Yellow
            Invoke-AzCli -Args @('grafana', 'folder', 'delete', '-g', $ResourceGroupName, '-n', $GrafanaName, '--folder', $uid, '-o', 'none') -AllowFailure | Out-Null
        }
    }

    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) 'mtogo-amg-ci'
    if (-not (Test-Path $tmp)) { New-Item -ItemType Directory -Path $tmp | Out-Null }

    foreach ($f in $files) {
        $content = Get-Content -Raw -Path $f.FullName -Encoding UTF8
        if ($DatasourceUid -and $DatasourceUid -ne 'prometheus') {
            $content = $content -replace '"uid"\s*:\s*"prometheus"', ('"uid": "' + $DatasourceUid + '"')
        }

        $dashboardObj = $null
        try { $dashboardObj = ($content | ConvertFrom-Json) } catch { Fail "Failed to parse dashboard JSON: $($f.FullName)" }

        $wrapped = @{ dashboard = $dashboardObj; message = 'Imported by CI provisioning' }
        $wrappedPath = Join-Path $tmp ("azurewrap-${GrafanaName}-$($f.Name)")
        Write-Utf8NoBom -Path $wrappedPath -Value ($wrapped | ConvertTo-Json -Depth 100)

        Write-Host "Importing dashboard: $($f.Name) -> ${GrafanaName}" -ForegroundColor Cyan
        Invoke-AzCli -Args @('grafana', 'dashboard', 'create', '-g', $ResourceGroupName, '-n', $GrafanaName, '--definition', ('@' + $wrappedPath), '--overwrite', 'true', '-o', 'none') | Out-Null
    }

    # Then: delete any remaining non-repo dashboards.
    $dashList = Invoke-AzCli -Args @('grafana', 'dashboard', 'list', '-g', $ResourceGroupName, '-n', $GrafanaName, '--query', '[].{uid:uid,title:title}', '-o', 'json')
    $dashboards = @()
    try { $dashboards = ($dashList.StdOut | ConvertFrom-Json) } catch { $dashboards = @() }

    foreach ($d in $dashboards) {
        if (-not $d.uid) { continue }
        if ($allowedUids.Contains([string]$d.uid)) { continue }

        Write-Host "Deleting non-repo dashboard from ${GrafanaName}: $($d.title) ($($d.uid))" -ForegroundColor Yellow
        Invoke-AzCli -Args @('grafana', 'dashboard', 'delete', '-g', $ResourceGroupName, '-n', $GrafanaName, '--dashboard', [string]$d.uid, '-o', 'none') -AllowFailure | Out-Null
    }

    # Assert strictness.
    $dashList2 = Invoke-AzCli -Args @('grafana', 'dashboard', 'list', '-g', $ResourceGroupName, '-n', $GrafanaName, '--query', '[].{uid:uid,title:title}', '-o', 'json') -AllowFailure
    $after = @()
    try { $after = ($dashList2.StdOut | ConvertFrom-Json) } catch { $after = @() }

    $remainingNonRepo = @($after | Where-Object { $_.uid -and -not $allowedUids.Contains([string]$_.uid) })
    if ($remainingNonRepo.Count -gt 0) {
        $names = ($remainingNonRepo | Sort-Object title | ForEach-Object { "{0} ({1})" -f $_.title, $_.uid }) -join '; '
        Fail "Non-repo dashboards still present in '${GrafanaName}' after cleanup: ${names}"
    }
}

function Ensure-KpiDiscordAlerting([string]$GrafanaName) {
    if (-not $DiscordWebhookUrl -or $DiscordWebhookUrl.Trim().Length -eq 0) {
        Fail "KPI alerting requires DISCORD_WEBHOOK_URL to be set."
    }

    $serviceAccountName = 'mtogo-provisioner'
    Ensure-ServiceAccountAdmin -GrafanaName $GrafanaName -ServiceAccountName $serviceAccountName

    $tokenObj = New-GrafanaToken -GrafanaName $GrafanaName -ServiceAccountName $serviceAccountName
    try {
        $base = Get-GrafanaEndpoint -GrafanaName $GrafanaName
        $headers = @{ Authorization = "Bearer $($tokenObj.Token)"; 'Content-Type' = 'application/json'; Accept = 'application/json' }

        $contactPointName = 'discord-business-alerts'

        $existing = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/contact-points") -Headers $headers
        $existingList = @($existing)
        $existingCp = $existingList | Where-Object { $_.name -eq $contactPointName } | Select-Object -First 1

        $cpBody = @{ name = $contactPointName; type = 'discord'; settings = @{ url = $DiscordWebhookUrl }; disableResolveMessage = $false }

        if ($existingCp -and ($existingCp.PSObject.Properties.Name -contains 'uid') -and $existingCp.uid) {
            $cpBody.uid = $existingCp.uid
            Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/contact-points/$($existingCp.uid)") -Headers $headers -Body ($cpBody | ConvertTo-Json -Depth 20) | Out-Null
        }
        else {
            Invoke-RestMethod -Method Post -Uri ("$base/api/v1/provisioning/contact-points") -Headers $headers -Body ($cpBody | ConvertTo-Json -Depth 20) | Out-Null
        }

        $policy = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/policies") -Headers $headers
        if ($null -eq $policy) { $policy = @{} }
        if (-not ($policy.PSObject.Properties.Name -contains 'group_by')) {
            $policy | Add-Member -NotePropertyName 'group_by' -NotePropertyValue @('grafana_folder', 'alertname')
        }
        $policy.receiver = $contactPointName
        Invoke-RestMethod -Method Put -Uri ("$base/api/v1/provisioning/policies") -Headers $headers -Body ($policy | ConvertTo-Json -Depth 50) | Out-Null
    }
    finally {
        Remove-GrafanaToken -GrafanaName $GrafanaName -ServiceAccountName $serviceAccountName -TokenId $tokenObj.TokenId
    }
}

function New-StableGrafanaUid([string]$Prefix, [string]$Name) {
    $slug = ($Name.ToLowerInvariant() -replace '[^a-z0-9_-]', '-')
    $slug = ($slug -replace '-{2,}', '-')
    $slug = $slug.Trim('-')
    if (-not $slug) { $slug = 'rule' }

    $uid = $Prefix + $slug
    if ($uid.Length -gt 40) { $uid = $uid.Substring(0, 40) }
    return $uid
}

function Parse-PrometheusAlertRulesYml([string]$Path) {
    if (-not (Test-Path $Path)) { Fail "Alert rules file not found: $Path" }

    $lines = @(Get-Content -Path $Path -Encoding UTF8)
    $groups = @()

    $currentGroup = $null
    $inRules = $false
    $i = 0

    while ($i -lt $lines.Count) {
        $line = $lines[$i]

        $mGroup = [regex]::Match($line, '^\s*-\s*name:\s*(?<name>[^#\r\n]+)')
        if ($mGroup.Success -and ($line -match '^\s{2}-\s*name:')) {
            if ($currentGroup) { $groups += $currentGroup }
            $currentGroup = [pscustomobject]@{ name = $mGroup.Groups['name'].Value.Trim().Trim('"'); interval = $null; rules = @() }
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
            if ($line -match '^\s*rules:\s*$') { $inRules = $true; $i++; continue }
        }

        if ($currentGroup -and $inRules) {
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
                    if ($l -match '^\s{6}-\s*alert:' -or $l -match '^\s{2}-\s*name:') { break }

                    $mFor = [regex]::Match($l, '^\s*for:\s*(?<v>[^#\r\n]+)')
                    if ($mFor.Success) { $for = $mFor.Groups['v'].Value.Trim(); $i++; continue }

                    $mExprBlock = [regex]::Match($l, '^(?<indent>\s*)expr:\s*\|\s*$')
                    if ($mExprBlock.Success) {
                        $exprIndent = $mExprBlock.Groups['indent'].Value.Length
                        $blockIndent = $exprIndent + 2

                        $i++
                        $exprLines = @()
                        while ($i -lt $lines.Count) {
                            $el = $lines[$i]
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

                    $mExprSingle = [regex]::Match($l, '^\s*expr:\s*(?<v>[^#\r\n]+)')
                    if ($mExprSingle.Success) {
                        $val = $mExprSingle.Groups['v'].Value.Trim()
                        if ($val -and $val -ne '|') { $expr = $val.Trim().Trim('"').Trim("'") }
                        $i++
                        continue
                    }

                    if ($l -match '^\s*labels:\s*$') {
                        $i++
                        while ($i -lt $lines.Count) {
                            $kv = $lines[$i]
                            if ($kv -match '^\s{10}[A-Za-z0-9_\-]+:\s*') {
                                $parts = $kv.Trim().Split(':', 2)
                                $k = $parts[0].Trim(); $v = $parts[1].Trim().Trim('"')
                                $labels[$k] = $v
                                $i++
                                continue
                            }
                            break
                        }
                        continue
                    }

                    if ($l -match '^\s*annotations:\s*$') {
                        $i++
                        while ($i -lt $lines.Count) {
                            $al = $lines[$i]

                            $mSummary = [regex]::Match($al, '^\s{10}summary:\s*(?<v>.+)$')
                            if ($mSummary.Success) { $annotations['summary'] = $mSummary.Groups['v'].Value.Trim().Trim('"'); $i++; continue }

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

                $currentGroup.rules += [pscustomobject]@{ alertName = $ruleName; expr = $expr; for = $for; labels = $labels; annotations = $annotations }
                continue
            }
        }

        $i++
    }

    if ($currentGroup) { $groups += $currentGroup }
    return $groups
}

function Ensure-GrafanaFolder([string]$GrafanaEndpoint, [hashtable]$Headers, [string]$FolderUid, [string]$FolderTitle) {
    $base = $GrafanaEndpoint.TrimEnd('/')
    $existing = Invoke-RestMethod -Method Get -Uri ("$base/api/folders") -Headers $Headers
    $list = @($existing)
    $match = $list | Where-Object { $_.uid -eq $FolderUid -or $_.title -eq $FolderTitle } | Select-Object -First 1
    if ($match) { return $match.uid }

    $body = @{ uid = $FolderUid; title = $FolderTitle } | ConvertTo-Json -Depth 5
    $created = Invoke-RestMethod -Method Post -Uri ("$base/api/folders") -Headers $Headers -Body $body
    if ($created -and $created.uid) { return $created.uid }
    return $FolderUid
}

function Import-KpiAlertRules([string]$GrafanaName, [string]$PrometheusDatasourceUid) {
    $rulesPath = Join-Path $RepoRoot 'monitoring/prometheus/alert_rules.yml'
    $groups = Parse-PrometheusAlertRulesYml -Path $rulesPath
    if (-not $groups -or $groups.Count -eq 0) { Fail "No alert rule groups parsed from: $rulesPath" }

    $serviceAccountName = 'mtogo-provisioner'
    Ensure-ServiceAccountAdmin -GrafanaName $GrafanaName -ServiceAccountName $serviceAccountName

    $tokenObj = New-GrafanaToken -GrafanaName $GrafanaName -ServiceAccountName $serviceAccountName
    try {
        $base = Get-GrafanaEndpoint -GrafanaName $GrafanaName
        $headers = @{ Authorization = "Bearer $($tokenObj.Token)"; 'Content-Type' = 'application/json'; Accept = 'application/json' }

        $folderUid = Ensure-GrafanaFolder -GrafanaEndpoint $base -Headers $headers -FolderUid 'mtogo' -FolderTitle 'MToGo'
        $existingRules = @()
        try { $existingRules = Invoke-RestMethod -Method Get -Uri ("$base/api/v1/provisioning/alert-rules") -Headers $headers } catch { $existingRules = @() }
        $existingList = @($existingRules)

        foreach ($g in $groups) {
            $ruleGroupName = 'mtogo-kpi-' + $g.name
            foreach ($r in $g.rules) {
                if (-not $r.expr) { continue }

                $uid = New-StableGrafanaUid -Prefix 'mtogo-kpi-' -Name $r.alertName
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
                        datasourceUid = $PrometheusDatasourceUid
                        model = @{ refId = 'A'; expr = $r.expr; intervalMs = 1000; maxDataPoints = 43200 }
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
                if ($r.for -and ("$($r.for)").Trim().Length -gt 0) { $forDuration = ("$($r.for)").Trim() }

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
        Remove-GrafanaToken -GrafanaName $GrafanaName -ServiceAccountName $serviceAccountName -TokenId $tokenObj.TokenId
    }
}

# -------------------
# Main
# -------------------

Ensure-Command -Name 'az'
Ensure-AzExtension -Name 'amg'

Write-Host "Waiting for Azure Managed Grafana instances to be ready..." -ForegroundColor Yellow
Invoke-AzCli -Args @('grafana', 'wait', '-g', $ResourceGroupName, '-n', $KpiGrafanaName, '--created', '--timeout', '900', '-o', 'none') | Out-Null
Invoke-AzCli -Args @('grafana', 'wait', '-g', $ResourceGroupName, '-n', $SloGrafanaName, '--created', '--timeout', '900', '-o', 'none') | Out-Null

$kpiDsUid = Get-OrCreate-PrometheusDatasourceUid -GrafanaName $KpiGrafanaName
$sloDsUid = Get-OrCreate-PrometheusDatasourceUid -GrafanaName $SloGrafanaName
if (-not $kpiDsUid) { $kpiDsUid = 'prometheus' }
if (-not $sloDsUid) { $sloDsUid = 'prometheus' }

Write-Host "Provisioning KPI alerting (Discord + alert rules)..." -ForegroundColor Yellow
Ensure-KpiDiscordAlerting -GrafanaName $KpiGrafanaName
Import-KpiAlertRules -GrafanaName $KpiGrafanaName -PrometheusDatasourceUid $kpiDsUid

Write-Host "Syncing dashboards with strict repo-only enforcement..." -ForegroundColor Yellow
$kpiDashDir = Join-Path $RepoRoot 'monitoring/grafana/dashboards'
$sloDashDir = Join-Path $RepoRoot 'monitoring-infrastructure/grafana/dashboards'

Import-DashboardsStrict -GrafanaName $KpiGrafanaName -DashboardDirectory $kpiDashDir -DatasourceUid $kpiDsUid
Import-DashboardsStrict -GrafanaName $SloGrafanaName -DashboardDirectory $sloDashDir -DatasourceUid $sloDsUid

Write-Host "Grafana provisioning complete." -ForegroundColor Green
