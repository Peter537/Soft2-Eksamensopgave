# ===========================================
# Variable Definitions (Local)
# ===========================================

variable "kubeconfig_path" {
  description = "Path to kubeconfig file"
  type        = string
  default     = "~/.kube/config"
}

variable "kubeconfig_context" {
  description = "Kubernetes context to use (e.g., docker-desktop, minikube)"
  type        = string
  default     = "" # Empty = use current context
}

variable "image_registry" {
  description = "Container registry prefix (e.g., mtogo for local images)"
  type        = string
  default     = "mtogo"
}

variable "image_tag" {
  description = "Docker image tag to deploy"
  type        = string
  default     = "latest"
}

variable "environment" {
  description = "Environment name (e.g., dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "postgres_admin_username" {
  description = "PostgreSQL admin username"
  type        = string
  default     = "mtogo"
}

variable "postgres_admin_password" {
  description = "PostgreSQL admin password"
  type        = string
  default     = "mtogo_password"
  sensitive   = true
}

variable "install_ingress" {
  description = "Whether to install NGINX ingress controller"
  type        = bool
  default     = true
}

variable "install_monitoring" {
  description = "Whether to install Prometheus/Alertmanager and two Grafana instances in the local Kubernetes cluster"
  type        = bool
  default     = true
}

variable "discord_webhook_url" {
  description = "Discord webhook URL for alert notifications (used by local Alertmanager when install_monitoring=true)."
  type        = string
  default     = null
  sensitive   = true
}

variable "grafana_kpi_admin_username" {
  description = "Admin username for KPI Grafana (local Kubernetes)"
  type        = string
  default     = "kpi_admin"
}

variable "grafana_kpi_admin_password" {
  description = "Admin password for KPI Grafana (local Kubernetes)"
  type        = string
  default     = "admin"
  sensitive   = true
}

variable "grafana_slo_admin_username" {
  description = "Admin username for SLO Grafana (local Kubernetes)"
  type        = string
  default     = "slo_admin"
}

variable "grafana_slo_admin_password" {
  description = "Admin password for SLO Grafana (local Kubernetes)"
  type        = string
  default     = "admin"
  sensitive   = true
}

variable "management_username" {
  description = "Initial ManagementService admin username (local dev)"
  type        = string
  default     = "admin"
}

variable "management_password" {
  description = "Initial ManagementService admin password (local dev)"
  type        = string
  default     = "admin"
  sensitive   = true
}

variable "management_name" {
  description = "Display name for the seeded management user"
  type        = string
  default     = "Management Admin"
}
