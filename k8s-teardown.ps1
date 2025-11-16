# Kubernetes Teardown Script
# This script removes all Kubernetes resources

Write-Host "🗑️  Tearing down Kubernetes deployment..." -ForegroundColor Yellow
Write-Host ""

kubectl delete namespace kafka-demo

Write-Host ""
Write-Host "✅ All resources deleted!" -ForegroundColor Green
Write-Host ""
Write-Host "🔄 To redeploy, run: .\k8s-deploy.ps1" -ForegroundColor Cyan
Write-Host ""
