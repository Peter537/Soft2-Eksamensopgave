# ===========================================
# MToGo Local Terraform Deployment Script
# ===========================================
# This script builds Docker images and deploys to local Kubernetes
# using Terraform.
#
# Prerequisites:
#   - Docker Desktop with Kubernetes enabled
#   - Terraform installed
#
# Usage:
#   .\deploy-local.ps1           # Deploy only
#   .\deploy-local.ps1 -Build    # Build images first, then deploy

param(
    [switch]$Build,
    [switch]$Destroy,
    [string]$Context = ""
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  MToGo Local Terraform Deployment" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

# Check Terraform
if (-not (Get-Command terraform -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Terraform is not installed" -ForegroundColor Red
    Write-Host "Install from: https://www.terraform.io/downloads" -ForegroundColor Yellow
    exit 1
}

# Check kubectl
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "Error: kubectl is not installed" -ForegroundColor Red
    exit 1
}

# Check Kubernetes cluster
$kubeContext = kubectl config current-context 2>$null
if (-not $kubeContext) {
    Write-Host "Error: No Kubernetes context found" -ForegroundColor Red
    Write-Host "Make sure Docker Desktop Kubernetes is enabled or Minikube is running" -ForegroundColor Yellow
    exit 1
}
Write-Host "Using Kubernetes context: $kubeContext" -ForegroundColor Green

# Handle destroy
if ($Destroy) {
    Write-Host "`nDestroying local deployment..." -ForegroundColor Yellow
    Set-Location "$RootDir\terraform\local"
    terraform destroy -auto-approve
    Write-Host "`nLocal deployment destroyed!" -ForegroundColor Green
    exit 0
}

# Build images if requested
if ($Build) {
    Write-Host "`nBuilding Docker images..." -ForegroundColor Yellow
    & "$RootDir\k8s\build-images.ps1" -Registry "mtogo" -Tag "latest"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error building images" -ForegroundColor Red
        exit 1
    }
}

# Initialize and apply Terraform
Write-Host "`nInitializing Terraform..." -ForegroundColor Yellow
Set-Location "$RootDir\terraform\local"

terraform init

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform init failed" -ForegroundColor Red
    exit 1
}

Write-Host "`nApplying Terraform configuration..." -ForegroundColor Yellow

$tfArgs = @("-auto-approve")
if ($Context) {
    $tfArgs += "-var=kubeconfig_context=$Context"
}

terraform apply @tfArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Terraform apply failed" -ForegroundColor Red
    exit 1
}

# Wait for deployments
Write-Host "`nWaiting for deployments to be ready..." -ForegroundColor Yellow
kubectl wait --for=condition=available --timeout=300s deployment --all -n mtogo

# Show status
Write-Host "`n================================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

Write-Host "`nPod Status:" -ForegroundColor Cyan
kubectl get pods -n mtogo

Write-Host "`nAccess the application:" -ForegroundColor Cyan
Write-Host "  Website:  kubectl port-forward -n mtogo svc/website 8081:8080" -ForegroundColor White
Write-Host "            Then open: http://localhost:8081" -ForegroundColor Gray
Write-Host ""
Write-Host "  API:      kubectl port-forward -n mtogo svc/gateway 8080:8080" -ForegroundColor White
Write-Host "            Then open: http://localhost:8080/api" -ForegroundColor Gray

Write-Host "`nTo destroy:" -ForegroundColor Yellow
Write-Host "  .\terraform\local\deploy-local.ps1 -Destroy" -ForegroundColor White
