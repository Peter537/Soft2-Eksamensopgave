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

variable "registry_server" {
  description = "Container registry server for image pull secret (e.g., ghcr.io)"
  type        = string
  default     = ""
}

variable "registry_username" {
  description = "Container registry username for image pull secret"
  type        = string
  default     = ""
}

variable "registry_password" {
  description = "Container registry password/token for image pull secret"
  type        = string
  default     = ""
  sensitive   = true
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

variable "management_username" {
  description = "Initial ManagementService admin username (required)"
  type        = string
}

variable "management_password" {
  description = "Initial ManagementService admin password (required)"
  type        = string
  sensitive   = true
}

variable "management_name" {
  description = "Display name for the seeded management user"
  type        = string
  default     = "Management Admin"
}

variable "grafana_url" {
  description = "Public Grafana URL to show in the Website management dashboard"
  type        = string
  default     = ""
}

# ===========================================
# Scaling
# ===========================================

variable "service_replicas" {
  description = "Replica count for the MToGo application services (gateway, website, backend services, websocket services, etc)."
  type        = number
  default     = 1
}

# ===========================================
# Ingress HTTPS/TLS (self-signed) configuration
# ===========================================

variable "ingress_host" {
  description = "Public host used to access the ingress (IP address for Azure, typically localhost for local clusters). This is used for Ingress host rules and for generating the self-signed certificate SANs via ingress_cert_* variables."
  type        = string
  default     = "localhost"
}

variable "ingress_load_balancer_ip" {
  description = "Optional static LoadBalancer IP to assign to ingress-nginx (recommended for Azure so the TLS cert can match the IP). Leave empty for local clusters."
  type        = string
  default     = ""
}

variable "ingress_tls_secret_name" {
  description = "Kubernetes TLS secret name used by the mtogo ingress"
  type        = string
  default     = "mtogo-ingress-tls"
}

variable "ingress_enable_ssl_redirect" {
  description = "Whether ingress should redirect HTTP to HTTPS"
  type        = bool
  default     = true
}

variable "ingress_enable_hsts" {
  description = "Whether ingress should emit Strict-Transport-Security headers"
  type        = bool
  default     = true
}

variable "ingress_hsts_max_age" {
  description = "HSTS max-age in seconds"
  type        = number
  default     = 31536000
}

variable "ingress_cert_ip_sans" {
  description = "IP Subject Alternative Names to embed in the self-signed TLS certificate. For IP-only access, include the ingress public IP here."
  type        = list(string)
  default     = ["127.0.0.1"]
}

variable "ingress_cert_dns_sans" {
  description = "DNS Subject Alternative Names to embed in the self-signed TLS certificate (e.g., localhost)."
  type        = list(string)
  default     = ["localhost"]
}
