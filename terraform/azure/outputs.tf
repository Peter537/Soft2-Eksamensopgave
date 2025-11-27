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

# Get ingress IP from the nginx service
data "kubernetes_service" "nginx_ingress" {
  metadata {
    name      = "ingress-nginx-controller"
    namespace = "ingress-nginx"
  }

  depends_on = [module.mtogo_app]
}

output "ingress_ip" {
  description = "IP address of the ingress controller load balancer"
  value       = try(data.kubernetes_service.nginx_ingress.status[0].load_balancer[0].ingress[0].ip, "pending")
}

output "website_url" {
  description = "URL for the website"
  value       = "http://${try(data.kubernetes_service.nginx_ingress.status[0].load_balancer[0].ingress[0].ip, "pending")}/"
}

output "api_url" {
  description = "URL for the API gateway"
  value       = "http://${try(data.kubernetes_service.nginx_ingress.status[0].load_balancer[0].ingress[0].ip, "pending")}/api"
}

output "legacy_api_url" {
  description = "URL for the legacy API"
  value       = "http://${try(data.kubernetes_service.nginx_ingress.status[0].load_balancer[0].ingress[0].ip, "pending")}/legacy"
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
       Website: http://${try(data.kubernetes_service.nginx_ingress.status[0].load_balancer[0].ingress[0].ip, "<pending>")}
       API:     http://${try(data.kubernetes_service.nginx_ingress.status[0].load_balancer[0].ingress[0].ip, "<pending>")}/api
    
    EOT
}
