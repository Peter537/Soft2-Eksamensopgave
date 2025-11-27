# ===========================================
# MToGo Kubernetes Teardown Script (PowerShell)
# ===========================================

$ErrorActionPreference = "Stop"

$Namespace = "mtogo"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  MToGo Kubernetes Teardown" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "WARNING: This will delete all MToGo resources!" -ForegroundColor Red
Write-Host ""

$confirm = Read-Host "Are you sure you want to continue? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Deleting namespace '$Namespace' and all resources..." -ForegroundColor Yellow

kubectl delete namespace $Namespace --ignore-not-found

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Teardown Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
