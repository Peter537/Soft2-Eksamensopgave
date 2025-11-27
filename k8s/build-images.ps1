# ===========================================
# MToGo Docker Image Build Script (PowerShell)
# ===========================================

param(
    [string]$Registry = "mtogo",
    [string]$Tag = "latest",
    [switch]$Push
)

$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  MToGo Docker Image Build" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Registry: $Registry"
Write-Host "Tag: $Tag"
Write-Host ""

# Define services and their Dockerfile paths
$services = @(
    @{ Name = "legacy-mtogo"; Context = "LegacyMToGo"; Dockerfile = "LegacyMToGo/Dockerfile" },
    @{ Name = "gateway"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.Gateway/Dockerfile" },
    @{ Name = "website"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.Website/Dockerfile" },
    @{ Name = "order-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.OrderService/Dockerfile" },
    @{ Name = "customer-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.CustomerService/Dockerfile" },
    @{ Name = "agent-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.AgentService/Dockerfile" },
    @{ Name = "agent-bonus-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.AgentBonusService/Dockerfile" },
    @{ Name = "feedback-hub-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.FeedbackHubService/Dockerfile" },
    @{ Name = "notification-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.NotificationService/Dockerfile" },
    @{ Name = "partner-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.PartnerService/Dockerfile" },
    @{ Name = "websocket-agent-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.WebSocketAgentService/Dockerfile" },
    @{ Name = "websocket-customer-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.WebSocketCustomerService/Dockerfile" },
    @{ Name = "websocket-partner-service"; Context = "MToGo"; Dockerfile = "MToGo/src/MToGo.WebSocketPartnerService/Dockerfile" }
)

$total = $services.Count
$current = 0

foreach ($service in $services) {
    $current++
    $imageName = "$Registry/$($service.Name):$Tag"
    $contextPath = Join-Path $RootDir $service.Context
    $dockerfilePath = Join-Path $RootDir $service.Dockerfile
    
    Write-Host ""
    Write-Host "[$current/$total] Building $imageName..." -ForegroundColor Yellow
    
    docker build -t $imageName -f $dockerfilePath $contextPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error building $imageName" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Successfully built $imageName" -ForegroundColor Green
    
    if ($Push) {
        Write-Host "Pushing $imageName..." -ForegroundColor Yellow
        docker push $imageName
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error pushing $imageName" -ForegroundColor Red
            exit 1
        }
        Write-Host "Successfully pushed $imageName" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  All images built successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green

if (-not $Push) {
    Write-Host ""
    Write-Host "To push images to a registry, run:" -ForegroundColor Cyan
    Write-Host "  .\build-images.ps1 -Registry <your-registry> -Push"
}
