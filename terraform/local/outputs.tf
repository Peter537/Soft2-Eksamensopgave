# ===========================================
# Outputs (Local)
# ===========================================

# Best-effort discovery of the ingress-nginx address.
# Note: On some local clusters (e.g. minikube without tunnel), this may not yield an external IP.
data "kubernetes_service" "nginx_ingress" {
  count = var.install_ingress ? 1 : 0

  metadata {
    name      = "ingress-nginx-controller"
    namespace = "ingress-nginx"
  }

  depends_on = [module.mtogo_app]
}

locals {
  ingress_ip       = try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, null)
  ingress_hostname = try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].hostname, null)
  ingress_host     = coalesce(local.ingress_ip, local.ingress_hostname, "localhost")
}

output "namespace" {
  description = "Kubernetes namespace"
  value       = module.mtogo_app.namespace
}

output "services" {
  description = "Deployed services"
  value       = module.mtogo_app.services
}

output "ingress_host" {
  description = "Ingress host/IP to use for local access (best-effort; defaults to localhost)"
  value       = local.ingress_host
}

output "website_url" {
  description = "Website URL (via ingress)"
  value       = "http://${local.ingress_host}/"
}

output "api_url" {
  description = "API URL (via ingress)"
  value       = "http://${local.ingress_host}/api/v1/"
}

output "legacy_api_url" {
  description = "Legacy API URL (via ingress)"
  value       = "http://${local.ingress_host}/legacy"
}

output "endpoints" {
  description = "Primary endpoints for the local Kubernetes deployment"
  value = {
    website    = "http://${local.ingress_host}/"
    api        = "http://${local.ingress_host}/api/v1/"
    legacy_api = "http://${local.ingress_host}/legacy"

    # Monitoring (installed by local Terraform into the cluster)
    grafana_kpi  = "http://localhost:3000"
    grafana_slo  = "http://localhost:3001"
    alerting     = "http://localhost:3000"
    prometheus   = "http://localhost:9090"
    alertmanager = "http://localhost:9093"
  }
}
