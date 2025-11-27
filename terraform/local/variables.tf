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
  default     = ""  # Empty = use current context
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
