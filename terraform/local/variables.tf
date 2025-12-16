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
