# ===========================================
# Outputs (Local)
# ===========================================

output "namespace" {
  description = "Kubernetes namespace"
  value       = module.mtogo_app.namespace
}

output "services" {
  description = "Deployed services"
  value       = module.mtogo_app.services
}

output "access_instructions" {
  description = "How to access the application"
  value       = <<-EOT
    
    ============================================
    Local Kubernetes Access Instructions
    ============================================

    Default: Using Ingress (Docker Desktop)
    ----------------------------------------
    Website:    http://localhost/
    API:        http://localhost/api/v1/
    Legacy API: http://localhost/legacy
    
    Useful Commands
    ----------------------------------------
    # View all pods
    kubectl get pods -n mtogo
    
    # View logs
    kubectl logs -f deployment/order-service -n mtogo
    
    # Check pod status
    kubectl describe pod <pod-name> -n mtogo
    
    EOT
}
