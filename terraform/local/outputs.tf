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

output "ingress_host" {
  description = "Ingress host used for local HTTPS access (matches the self-signed certificate SAN)"
  value       = "localhost"
}

output "website_url" {
  description = "Website URL (via ingress)"
  value       = "https://localhost/"
}

output "api_url" {
  description = "API URL (via ingress)"
  value       = "https://localhost/api/v1/"
}

output "legacy_api_url" {
  description = "Legacy API URL (via ingress)"
  value       = "https://localhost/legacy"
}

output "endpoints" {
  description = "Primary endpoints for the local Kubernetes deployment"
  value = {
    website    = "https://localhost/"
    api        = "https://localhost/api/v1/"
    legacy_api = "https://localhost/legacy"

    # Monitoring (installed by local Terraform into the cluster)
    grafana_kpi  = "http://localhost:3000"
    grafana_slo  = "http://localhost:3001"
    alerting     = "http://localhost:3000"
    prometheus   = "http://localhost:9090"
    alertmanager = "http://localhost:9093"
  }
}
