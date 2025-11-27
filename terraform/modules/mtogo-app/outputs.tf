# ===========================================
# Module Outputs
# ===========================================

output "namespace" {
  description = "Kubernetes namespace where the application is deployed"
  value       = kubernetes_namespace.mtogo.metadata[0].name
}

output "services" {
  description = "Map of deployed services"
  value = {
    for name, svc in kubernetes_service.services : name => {
      name = svc.metadata[0].name
      port = 8080
    }
  }
}

output "ingress_name" {
  description = "Name of the ingress resource"
  value       = kubernetes_ingress_v1.mtogo.metadata[0].name
}
