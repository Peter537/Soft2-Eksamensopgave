# ===========================================
# MToGo Azure Infrastructure - Terraform
# ===========================================
# This configuration deploys MToGo to Azure Kubernetes Service (AKS).
# It uses the shared mtogo-app module for application deployment.
#
# Usage:
#   cd terraform/azure
#   terraform init
#   terraform apply -var-file=terraform.tfvars
#
# See terraform.tfvars.example for required variables.

terraform {
  required_version = ">= 1.0.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.0"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }

  # Backend for storing state in Azure - uncomment when needed
  # backend "azurerm" {
  #   resource_group_name  = "mtogo-terraform-state"
  #   storage_account_name = "mtogotfstate"
  #   container_name       = "tfstate"
  #   key                  = "mtogo.terraform.tfstate"
  # }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

locals {
  kube_host                   = var.use_aks_kubeconfig ? azurerm_kubernetes_cluster.main.kube_config[0].host : "https://localhost"
  kube_client_certificate     = var.use_aks_kubeconfig ? base64decode(azurerm_kubernetes_cluster.main.kube_config[0].client_certificate) : ""
  kube_client_key             = var.use_aks_kubeconfig ? base64decode(azurerm_kubernetes_cluster.main.kube_config[0].client_key) : ""
  kube_cluster_ca_certificate = var.use_aks_kubeconfig ? base64decode(azurerm_kubernetes_cluster.main.kube_config[0].cluster_ca_certificate) : ""
}

# ===========================================
# Kubernetes Provider (connects to AKS)
# ===========================================

provider "kubernetes" {
  host                   = local.kube_host
  client_certificate     = local.kube_client_certificate
  client_key             = local.kube_client_key
  cluster_ca_certificate = local.kube_cluster_ca_certificate
}

provider "helm" {
  kubernetes {
    host                   = local.kube_host
    client_certificate     = local.kube_client_certificate
    client_key             = local.kube_client_key
    cluster_ca_certificate = local.kube_cluster_ca_certificate
  }
}

# ===========================================
# Container Registry Secret
# ===========================================

# NOTE: Registry secret is created inside the mtogo-app module so that
# all deployments depend on it (and we avoid rollouts failing before the
# secret exists).

# ===========================================
# MToGo Application Module
# ===========================================

module "mtogo_app" {
  source = "../modules/mtogo-app"

  environment             = var.environment
  namespace               = "mtogo"
  image_registry          = "ghcr.io/${lower(var.github_repository_owner)}"
  image_tag               = var.image_tag
  postgres_host           = azurerm_postgresql_flexible_server.main.fqdn
  postgres_admin_username = var.postgres_admin_username
  postgres_admin_password = var.postgres_admin_password
  postgres_ssl_mode       = "Require"
  registry_secret_name    = "ghcr-secret"
  registry_server         = "ghcr.io"
  registry_username       = var.ghcr_username
  registry_password       = var.ghcr_token

  # Azure provides managed services
  deploy_postgres            = false # Using Azure PostgreSQL
  deploy_kafka               = true  # Deploy Kafka in cluster
  install_ingress_controller = true
  kafka_bootstrap_servers    = "kafka:9092"

  # Public ingress is IP-only (no DNS). Bind ingress-nginx to a static Public IP
  # and generate a self-signed cert whose SAN contains the IP.
  ingress_load_balancer_ip    = azurerm_public_ip.ingress.ip_address
  ingress_host                = azurerm_public_ip.ingress.ip_address
  ingress_enable_ssl_redirect = true
  ingress_enable_hsts         = true
  ingress_hsts_max_age        = 31536000
  ingress_cert_ip_sans        = [azurerm_public_ip.ingress.ip_address]
  ingress_cert_dns_sans       = []

  management_username = var.management_username
  management_password = var.management_password
  management_name     = var.management_name

  # Website Management dashboard link target
  grafana_url = azurerm_dashboard_grafana.kpi.endpoint

  # AKS node pools (e.g., Standard_B2s_v2) can be CPU constrained; 3 replicas for every service
  # easily becomes unschedulable. Scale up later by increasing node size/count.
  service_replicas = 1

  seed_demo_data = var.seed_demo_data

  depends_on = [azurerm_kubernetes_cluster.main]
}
