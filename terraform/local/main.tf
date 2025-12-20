# ===========================================
# MToGo Local Kubernetes Deployment
# ===========================================
# This configuration deploys MToGo to a local Kubernetes cluster
# (Docker Desktop, Minikube, kind, etc.)
#
# It uses the SAME module as Azure, demonstrating how easy it is
# to switch between cloud providers.
#
# Prerequisites:
#   - Docker Desktop with Kubernetes enabled, OR
#   - Minikube, OR
#   - kind (Kubernetes in Docker)
#
# Usage:
#   cd terraform/local
#   terraform init
#   terraform apply
#
# Access (Docker Desktop + ingress-nginx):
#   Open http://localhost/
#   API is available under http://localhost/api/v1/

terraform {
  required_version = ">= 1.0.0"

  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.0"
    }
  }
}

# ===========================================
# Kubernetes Provider (Local Cluster)
# ===========================================
# Uses your current kubectl context

provider "kubernetes" {
  config_path    = var.kubeconfig_path
  config_context = var.kubeconfig_context
}

provider "helm" {
  kubernetes {
    config_path    = var.kubeconfig_path
    config_context = var.kubeconfig_context
  }
}

# ===========================================
# MToGo Application Module
# ===========================================
# Same module as Azure - just different configuration!

module "mtogo_app" {
  source = "../modules/mtogo-app"

  environment             = var.environment
  namespace               = "mtogo"
  image_registry          = var.image_registry
  image_tag               = var.image_tag
  postgres_host           = "mtogo-db" # In-cluster PostgreSQL
  postgres_admin_username = var.postgres_admin_username
  postgres_admin_password = var.postgres_admin_password
  postgres_ssl_mode       = "Disable" # No SSL for local
  registry_secret_name    = ""        # No auth needed for local images

  # Deploy infrastructure in cluster
  deploy_postgres            = true # Deploy PostgreSQL in cluster
  deploy_kafka               = true # Deploy Kafka in cluster
  install_ingress_controller = var.install_ingress
  kafka_bootstrap_servers    = "kafka:9092"

  management_username = var.management_username
  management_password = var.management_password
  management_name     = var.management_name
}
