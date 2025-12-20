# ===========================================
# Outputs
# ===========================================

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "aks_cluster_name" {
  description = "Name of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.name
}

output "aks_cluster_id" {
  description = "ID of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.id
}

output "kube_config" {
  description = "Kubernetes config for connecting to the cluster"
  value       = azurerm_kubernetes_cluster.main.kube_config_raw
  sensitive   = true
}

output "postgres_fqdn" {
  description = "Fully qualified domain name of the PostgreSQL server"
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "postgres_admin_username" {
  description = "Admin username for PostgreSQL"
  value       = var.postgres_admin_username
}

output "azure_monitor_workspace_id" {
  description = "Azure Monitor Workspace resource id (Managed Prometheus)"
  value       = azurerm_monitor_workspace.slo.id
}

output "azure_monitor_workspace_query_endpoint" {
  description = "Azure Monitor Workspace query endpoint"
  value       = azurerm_monitor_workspace.slo.query_endpoint
}

output "slo_grafana_endpoint" {
  description = "Azure Managed Grafana endpoint (SLO instance)"
  value       = azurerm_dashboard_grafana.slo.endpoint
}

output "slo_grafana_name" {
  description = "Azure Managed Grafana name (SLO instance)"
  value       = azurerm_dashboard_grafana.slo.name
}

output "slo_grafana_id" {
  description = "Azure Managed Grafana resource id (SLO instance)"
  value       = azurerm_dashboard_grafana.slo.id
}

output "kpi_grafana_endpoint" {
  description = "Azure Managed Grafana endpoint (KPI instance)"
  value       = azurerm_dashboard_grafana.kpi.endpoint
}

output "kpi_grafana_name" {
  description = "Azure Managed Grafana name (KPI instance)"
  value       = azurerm_dashboard_grafana.kpi.name
}

output "kpi_grafana_id" {
  description = "Azure Managed Grafana resource id (KPI instance)"
  value       = azurerm_dashboard_grafana.kpi.id
}

output "prometheus_query_endpoint" {
  description = "Prometheus-compatible query endpoint (Azure Monitor Workspace / Managed Prometheus)"
  value       = azurerm_monitor_workspace.slo.query_endpoint
}

# Get ingress IP from the nginx service
data "kubernetes_service" "nginx_ingress" {
  count = var.use_aks_kubeconfig ? 1 : 0

  metadata {
    name      = "ingress-nginx-controller"
    namespace = "ingress-nginx"
  }

  depends_on = [module.mtogo_app]
}

output "ingress_ip" {
  description = "IP address of the ingress controller load balancer"
  value       = try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")
}

output "website_url" {
  description = "URL for the website"
  value       = "http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")}/"
}

output "api_url" {
  description = "URL for the API gateway"
  value       = "http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")}/api/v1"
}

output "legacy_api_url" {
  description = "URL for the legacy API"
  value       = "http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")}/legacy"
}

output "endpoints" {
  description = "Primary endpoints for the Azure deployment"
  value = {
    website     = "http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")}/"
    api         = "http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")}/api/v1"
    legacy_api  = "http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "pending")}/legacy"
    grafana_kpi = azurerm_dashboard_grafana.kpi.endpoint
    grafana_slo = azurerm_dashboard_grafana.slo.endpoint
    alerting    = azurerm_dashboard_grafana.kpi.endpoint
    prometheus  = azurerm_monitor_workspace.slo.query_endpoint
  }
}

# Instructions for connecting to the cluster
output "connect_instructions" {
  description = "Instructions for connecting to the AKS cluster"
  value       = <<-EOT
    
    ============================================
    AKS Cluster Connection Instructions
    ============================================
    
    1. Install Azure CLI if not already installed
    
    2. Login to Azure:
       az login
    
    3. Get credentials for your AKS cluster:
       az aks get-credentials --resource-group ${azurerm_resource_group.main.name} --name ${azurerm_kubernetes_cluster.main.name}
    
    4. Verify connection:
       kubectl get nodes
    
    5. View all pods in the mtogo namespace:
       kubectl get pods -n mtogo
    
    6. Access the application:
         Website: http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "<pending>")}
         API:     http://${try(data.kubernetes_service.nginx_ingress[0].status[0].load_balancer[0].ingress[0].ip, "<pending>")}/api/v1
    
    EOT
}
