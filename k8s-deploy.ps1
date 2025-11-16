# Kubernetes Deployment Script for Kafka Order Demo
# This script builds Docker images and deploys everything to Kubernetes

Write-Host "[*] Starting Kubernetes Deployment..." -ForegroundColor Cyan
Write-Host ""

# Check if Kubernetes is running
Write-Host "Checking Kubernetes status..." -ForegroundColor Yellow
$dockerInfo = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Kubernetes is not running!" -ForegroundColor Red
    Write-Host "Please enable Kubernetes in Docker Desktop and try again." -ForegroundColor Yellow
    exit 1
}
Write-Host "[OK] Kubernetes is running" -ForegroundColor Green
Write-Host ""

# Check for existing deployment
$existingNamespace = kubectl get namespace kafka-demo 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[WARNING] Namespace 'kafka-demo' already exists!" -ForegroundColor Yellow
    Write-Host "   This will update the existing deployment." -ForegroundColor Yellow
    Write-Host "   To start fresh, run: .\k8s-teardown.ps1 first" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   Continue anyway? (Y/N): " -ForegroundColor Cyan -NoNewline
    $response = Read-Host
    if ($response -ne 'Y' -and $response -ne 'y') {
        Write-Host "Deployment cancelled." -ForegroundColor Yellow
        exit 0
    }
    Write-Host ""
}

# Build Docker images
Write-Host "[BUILD] Building Docker images..." -ForegroundColor Cyan
Write-Host ""

Write-Host "Building CentralHub (Gateway)..." -ForegroundColor Yellow
docker build -t centralhub:latest -f CentralHub.API/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host "Building OrderService..." -ForegroundColor Yellow
docker build -t orderservice:latest -f OrderService/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host "Building PartnerService..." -ForegroundColor Yellow
docker build -t partnerservice:latest -f PartnerService/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host "Building WebsocketPartnerService..." -ForegroundColor Yellow
docker build -t websocketpartnerservice:latest -f WebsocketPartnerService/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host "Building LocationService..." -ForegroundColor Yellow
docker build -t locationservice:latest -f LocationService/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host "Building NotificationService..." -ForegroundColor Yellow
docker build -t notificationservice:latest -f NotificationService/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host "Building Frontend..." -ForegroundColor Yellow
docker build -t frontend:latest -f KafkaOrderDemo/Dockerfile --build-arg BUILD_CONFIGURATION=Release .

Write-Host ""
Write-Host "[OK] All images built successfully!" -ForegroundColor Green
Write-Host ""

# Deploy to Kubernetes
Write-Host "[DEPLOY] Deploying to Kubernetes..." -ForegroundColor Cyan
Write-Host ""

kubectl apply -f k8s/

Write-Host ""
Write-Host "[WAIT] Waiting for pods to be ready..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=kafka -n kafka-demo --timeout=180s

Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "         DEPLOYMENT COMPLETE!                                  " -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Access your services at:" -ForegroundColor Cyan
Write-Host "   Frontend:                  http://localhost:30080" -ForegroundColor White
Write-Host "   CentralHub (Gateway):      http://localhost:30001" -ForegroundColor White
Write-Host "   OrderService:              http://localhost:30005" -ForegroundColor White
Write-Host "   PartnerService:            http://localhost:30002" -ForegroundColor White
Write-Host "   WebsocketPartnerService:   http://localhost:30003" -ForegroundColor White
Write-Host "   NotificationService:       http://localhost:30004" -ForegroundColor White
Write-Host ""
Write-Host "View all pods:" -ForegroundColor Cyan
Write-Host "   kubectl get pods -n kafka-demo" -ForegroundColor White
Write-Host ""
Write-Host "View logs:" -ForegroundColor Cyan
Write-Host "   kubectl logs -f deployment/centralhub -n kafka-demo" -ForegroundColor White
Write-Host "   kubectl logs -f deployment/orderservice -n kafka-demo" -ForegroundColor White
Write-Host "   kubectl logs -f deployment/partnerservice -n kafka-demo" -ForegroundColor White
Write-Host "   kubectl logs -f deployment/websocketpartnerservice -n kafka-demo" -ForegroundColor White
Write-Host "   kubectl logs -f deployment/notificationservice -n kafka-demo" -ForegroundColor White
Write-Host ""
Write-Host "To tear down everything:" -ForegroundColor Cyan
Write-Host "   .\k8s-teardown.ps1" -ForegroundColor White
Write-Host ""
