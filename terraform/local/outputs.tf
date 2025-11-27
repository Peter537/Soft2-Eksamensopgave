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
    
    Option 1: Port Forwarding (Recommended)
    ----------------------------------------
    # Website
    kubectl port-forward -n mtogo svc/website 8081:8080
    # Open: http://localhost:8081
    
    # API Gateway
    kubectl port-forward -n mtogo svc/gateway 8080:8080
    # Open: http://localhost:8080/api
    
    # Legacy API
    kubectl port-forward -n mtogo svc/legacy-mtogo 8082:8080
    # Open: http://localhost:8082
    
    Option 2: Using Ingress (if installed)
    ----------------------------------------
    # Add to /etc/hosts (or C:\Windows\System32\drivers\etc\hosts):
    127.0.0.1 mtogo.local
    
    # Get ingress IP:
    kubectl get svc -n ingress-nginx
    
    # Open: http://mtogo.local
    
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
