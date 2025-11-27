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
      version = "~> 3.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.0"
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

# ===========================================
# Kubernetes Provider (connects to AKS)
# ===========================================

provider "kubernetes" {
  host                   = azurerm_kubernetes_cluster.main.kube_config[0].host
  client_certificate     = base64decode(azurerm_kubernetes_cluster.main.kube_config[0].client_certificate)
  client_key             = base64decode(azurerm_kubernetes_cluster.main.kube_config[0].client_key)
  cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.main.kube_config[0].cluster_ca_certificate)
}

provider "helm" {
  kubernetes {
    host                   = azurerm_kubernetes_cluster.main.kube_config[0].host
    client_certificate     = base64decode(azurerm_kubernetes_cluster.main.kube_config[0].client_certificate)
    client_key             = base64decode(azurerm_kubernetes_cluster.main.kube_config[0].client_key)
    cluster_ca_certificate = base64decode(azurerm_kubernetes_cluster.main.kube_config[0].cluster_ca_certificate)
  }
}

# ===========================================
# Container Registry Secret
# ===========================================

resource "kubernetes_secret" "ghcr" {
  metadata {
    name      = "ghcr-secret"
    namespace = module.mtogo_app.namespace
  }

  type = "kubernetes.io/dockerconfigjson"

  data = {
    ".dockerconfigjson" = jsonencode({
      auths = {
        "ghcr.io" = {
          username = var.ghcr_username
          password = var.ghcr_token
          auth     = base64encode("${var.ghcr_username}:${var.ghcr_token}")
        }
      }
    })
  }

  depends_on = [module.mtogo_app]
}

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
  
  # Azure provides managed services
  deploy_postgres            = false  # Using Azure PostgreSQL
  deploy_kafka               = true   # Deploy Kafka in cluster
  install_ingress_controller = true
  kafka_bootstrap_servers    = "kafka:9092"

  depends_on = [azurerm_kubernetes_cluster.main]
}
