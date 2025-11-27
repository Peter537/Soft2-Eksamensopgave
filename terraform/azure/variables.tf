# ===========================================
# Variable Definitions
# ===========================================

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "West Europe"
}

variable "environment" {
  description = "Environment name (e.g., dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "mtogo"
}

variable "kubernetes_version" {
  description = "Kubernetes version for AKS"
  type        = string
  default     = "1.29"
}

variable "node_count" {
  description = "Initial/minimum number of nodes in the default node pool"
  type        = number
  default     = 2
}

variable "node_count_max" {
  description = "Maximum number of nodes when auto-scaling is enabled"
  type        = number
  default     = 5
}

variable "enable_auto_scaling" {
  description = "Enable cluster auto-scaling"
  type        = bool
  default     = true
}

variable "node_vm_size" {
  description = "VM size for the nodes"
  type        = string
  default     = "Standard_B2s"
}

variable "postgres_admin_username" {
  description = "Admin username for PostgreSQL"
  type        = string
  default     = "mtogoadmin"
}

variable "postgres_admin_password" {
  description = "Admin password for PostgreSQL"
  type        = string
  sensitive   = true
}

variable "ghcr_username" {
  description = "GitHub Container Registry username"
  type        = string
}

variable "ghcr_token" {
  description = "GitHub Container Registry token (PAT with read:packages)"
  type        = string
  sensitive   = true
}

variable "github_repository_owner" {
  description = "GitHub repository owner (lowercase)"
  type        = string
}

variable "image_tag" {
  description = "Docker image tag to deploy"
  type        = string
  default     = "latest"
}
