# ===========================================
# MToGo Kubernetes Application Module
# ===========================================
# This module contains the Kubernetes resources for the MToGo platform.
# It can be used with any Kubernetes cluster (local, Azure, AWS, GCP).
#
# Usage:
#   module "mtogo_app" {
#     source = "../modules/mtogo-app"
#     
#     environment             = "prod"
#     image_registry          = "ghcr.io/owner"
#     image_tag               = "latest"
#     postgres_host           = "postgres-host"
#     postgres_admin_username = <string>
#     postgres_admin_password = <string>
#     registry_secret_name    = "ghcr-secret"
#   }

terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = ">= 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = ">= 2.0"
    }
  }
}

# ===========================================
# Variables
# ===========================================

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "namespace" {
  description = "Kubernetes namespace for the application"
  type        = string
  default     = "mtogo"
}

variable "image_registry" {
  description = "Container registry prefix (e.g., ghcr.io/owner or docker.io/owner)"
  type        = string
}

variable "image_tag" {
  description = "Docker image tag to deploy"
  type        = string
  default     = "latest"
}

variable "postgres_host" {
  description = "PostgreSQL host address"
  type        = string
}

variable "postgres_admin_username" {
  description = "PostgreSQL admin username"
  type        = string
}

variable "postgres_admin_password" {
  description = "PostgreSQL admin password"
  type        = string
  sensitive   = true
}

variable "postgres_ssl_mode" {
  description = "PostgreSQL SSL mode (Require for Azure, Disable for local)"
  type        = string
  default     = "Disable"
}

variable "registry_secret_name" {
  description = "Name of the Kubernetes secret for container registry auth (empty for local images)"
  type        = string
  default     = ""
}

variable "install_ingress_controller" {
  description = "Whether to install NGINX ingress controller"
  type        = bool
  default     = true
}

variable "deploy_kafka" {
  description = "Whether to deploy Kafka in the cluster"
  type        = bool
  default     = true
}

variable "deploy_postgres" {
  description = "Whether to deploy PostgreSQL in the cluster (for local dev)"
  type        = bool
  default     = false
}

variable "kafka_bootstrap_servers" {
  description = "Kafka bootstrap servers address"
  type        = string
  default     = "kafka:9092"
}
