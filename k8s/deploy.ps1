# ===========================================
# MToGo Kubernetes Deployment Script (PowerShell)
# ===========================================

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Namespace = "mtogo"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  MToGo Kubernetes Deployment" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Check if kubectl is available
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "Error: kubectl is not installed or not in PATH" -ForegroundColor Red
    exit 1
}

# Check cluster connection
Write-Host "Checking cluster connection..."
try {
    kubectl cluster-info | Out-Null
} catch {
    Write-Host "Error: Cannot connect to Kubernetes cluster" -ForegroundColor Red
    exit 1
}

$context = kubectl config current-context
Write-Host "Connected to cluster: $context" -ForegroundColor Green
Write-Host ""

# Step 1: Create namespace
Write-Host "[1/5] Creating namespace..." -ForegroundColor Yellow
kubectl apply -f "$ScriptDir\namespace.yaml"

# Step 2: Deploy infrastructure (secrets, configmaps)
Write-Host "[2/5] Deploying infrastructure configuration..." -ForegroundColor Yellow
kubectl apply -f "$ScriptDir\infrastructure\configmap.yaml"
kubectl apply -f "$ScriptDir\infrastructure\secrets.yaml"

# Step 3: Deploy infrastructure services (PostgreSQL, Kafka)
Write-Host "[3/5] Deploying infrastructure services..." -ForegroundColor Yellow
kubectl apply -f "$ScriptDir\infrastructure\postgres.yaml"
kubectl apply -f "$ScriptDir\infrastructure\kafka.yaml"

# Wait for infrastructure to be ready
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=mtogo-db -n $Namespace --timeout=120s

Write-Host "Waiting for Kafka to be ready..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=kafka -n $Namespace --timeout=120s

# Step 4: Deploy application services
Write-Host "[4/5] Deploying application services..." -ForegroundColor Yellow
Get-ChildItem "$ScriptDir\services\*.yaml" | ForEach-Object {
    kubectl apply -f $_.FullName
}

# Step 5: Deploy ingress
Write-Host "[5/5] Deploying ingress..." -ForegroundColor Yellow
kubectl apply -f "$ScriptDir\ingress\ingress.yaml"

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

Write-Host "Waiting for pods to start (30 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host ""
Write-Host "Pod status:" -ForegroundColor Green
kubectl get pods -n $Namespace

Write-Host ""
Write-Host "Services:" -ForegroundColor Green
kubectl get svc -n $Namespace

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Access your application:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Website:     http://localhost:31081" -ForegroundColor White
Write-Host "  API Gateway: http://localhost:31080" -ForegroundColor White
Write-Host "  Legacy API:  http://localhost:31082" -ForegroundColor White
Write-Host ""
