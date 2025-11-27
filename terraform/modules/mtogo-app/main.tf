# ===========================================
# Namespace
# ===========================================

resource "kubernetes_namespace" "mtogo" {
  metadata {
    name = var.namespace
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
      "environment"               = var.environment
    }
  }
}

# ===========================================
# NGINX Ingress Controller (Optional)
# ===========================================

resource "helm_release" "nginx_ingress" {
  count = var.install_ingress_controller ? 1 : 0

  name             = "ingress-nginx"
  repository       = "https://kubernetes.github.io/ingress-nginx"
  chart            = "ingress-nginx"
  namespace        = "ingress-nginx"
  create_namespace = true
  version          = "4.10.0"

  set {
    name  = "controller.service.type"
    value = "LoadBalancer"
  }
}

# ===========================================
# Database Connection Secrets
# ===========================================

resource "kubernetes_secret" "db_credentials" {
  metadata {
    name      = "mtogo-db-secret"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
    }
  }

  data = {
    POSTGRES_USER     = var.postgres_admin_username
    POSTGRES_PASSWORD = var.postgres_admin_password
    POSTGRES_HOST     = var.postgres_host

    CONNECTION_STRING_ORDERS   = "Host=${var.postgres_host};Database=mtogo_orders;Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SSL Mode=${var.postgres_ssl_mode}"
    CONNECTION_STRING_AGENTS   = "Host=${var.postgres_host};Database=mtogo_agents;Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SSL Mode=${var.postgres_ssl_mode}"
    CONNECTION_STRING_FEEDBACK = "Host=${var.postgres_host};Database=mtogo_feedback;Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SSL Mode=${var.postgres_ssl_mode}"
    CONNECTION_STRING_PARTNERS = "Host=${var.postgres_host};Database=mtogo_partners;Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SSL Mode=${var.postgres_ssl_mode}"
    CONNECTION_STRING_LEGACY   = "Host=${var.postgres_host};Database=mtogo_legacy;Username=${var.postgres_admin_username};Password=${var.postgres_admin_password};SSL Mode=${var.postgres_ssl_mode}"
  }
}

# ===========================================
# ConfigMap
# ===========================================

resource "kubernetes_config_map" "mtogo" {
  metadata {
    name      = "mtogo-config"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
    }
  }

  data = {
    ASPNETCORE_ENVIRONMENT    = var.environment == "prod" ? "Production" : "Development"
    ASPNETCORE_URLS           = "http://+:8080"
    "Kafka__BootstrapServers" = var.kafka_bootstrap_servers
  }
}
