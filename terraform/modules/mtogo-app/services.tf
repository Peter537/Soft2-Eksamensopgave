# ===========================================
# MToGo Application Services
# ===========================================

locals {
  image_prefix = var.image_registry
  image_tag    = var.image_tag

  # Use image pull secret only if provided
  use_image_pull_secret = var.registry_secret_name != ""

  # Resource limits for services
  default_resources = {
    requests = {
      memory = "128Mi"
      cpu    = "100m"
    }
    limits = {
      memory = "256Mi"
      cpu    = "250m"
    }
  }

  # Wait for infrastructure if deployed locally
  depends_on_infra = var.deploy_kafka ? [kubernetes_deployment.kafka[0]] : []
}

# ===========================================
# Gateway ConfigMap
# ===========================================

resource "kubernetes_config_map" "gateway" {
  metadata {
    name      = "gateway-config"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/name" = "gateway"
    }
  }

  data = {
    "ReverseProxy__Clusters__orders-cluster__Destinations__orders-destination__Address"               = "http://order-service:8080"
    "ReverseProxy__Clusters__customers-cluster__Destinations__customers-destination__Address"         = "http://customer-service:8080"
    "ReverseProxy__Clusters__agents-cluster__Destinations__agents-destination__Address"               = "http://agent-service:8080"
    "ReverseProxy__Clusters__agentbonuses-cluster__Destinations__agentbonus-destination__Address"     = "http://agent-bonus-service:8080"
    "ReverseProxy__Clusters__feedback-cluster__Destinations__feedbackhub-destination__Address"        = "http://feedback-hub-service:8080"
    "ReverseProxy__Clusters__notifications-cluster__Destinations__notifications-destination__Address" = "http://notification-service:8080"
    "ReverseProxy__Clusters__partners-cluster__Destinations__partners-destination__Address"           = "http://partner-service:8080"
    "ReverseProxy__Clusters__wsagents-cluster__Destinations__wsagents-destination__Address"           = "http://websocket-agent-service:8080"
    "ReverseProxy__Clusters__wscustomers-cluster__Destinations__wscustomers-destination__Address"     = "http://websocket-customer-service:8080"
    "ReverseProxy__Clusters__wspartners-cluster__Destinations__wspartners-destination__Address"       = "http://websocket-partner-service:8080"
  }
}

# ===========================================
# Service Definitions
# ===========================================

# Helper for creating deployments
locals {
  services = {
    gateway = {
      image           = "mtogo-gateway"
      component       = "gateway"
      use_db          = false
      use_kafka       = true
      extra_config    = "gateway-config"
      extra_env       = {}
    }
    website = {
      image           = "mtogo-website"
      component       = "frontend"
      use_db          = false
      use_kafka       = false
      extra_config    = null
      extra_env       = { GatewayUrl = "http://gateway:8080" }
    }
    order-service = {
      image           = "mtogo-order"
      component       = "backend"
      use_db          = true
      db_key          = "CONNECTION_STRING_ORDERS"
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    customer-service = {
      image           = "mtogo-customerservice"
      component       = "backend"
      use_db          = false
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    agent-service = {
      image           = "mtogo-agentservice"
      component       = "backend"
      use_db          = true
      db_key          = "CONNECTION_STRING_AGENTS"
      use_kafka       = false
      extra_config    = null
      extra_env       = {}
    }
    agent-bonus-service = {
      image           = "mtogo-agentbonus"
      component       = "backend"
      use_db          = false
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    feedback-hub-service = {
      image           = "mtogo-feedbackhub"
      component       = "backend"
      use_db          = true
      db_key          = "CONNECTION_STRING_FEEDBACK"
      use_kafka       = false
      extra_config    = null
      extra_env       = {}
    }
    notification-service = {
      image           = "mtogo-notification"
      component       = "backend"
      use_db          = false
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    partner-service = {
      image           = "mtogo-partner"
      component       = "backend"
      use_db          = true
      db_key          = "CONNECTION_STRING_PARTNERS"
      use_kafka       = false
      extra_config    = null
      extra_env       = {}
    }
    websocket-agent-service = {
      image           = "mtogo-websocketagent"
      component       = "websocket"
      use_db          = false
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    websocket-customer-service = {
      image           = "mtogo-websocketcustomer"
      component       = "websocket"
      use_db          = false
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    websocket-partner-service = {
      image           = "mtogo-websocketpartner"
      component       = "websocket"
      use_db          = false
      use_kafka       = true
      extra_config    = null
      extra_env       = {}
    }
    legacy-mtogo = {
      image           = "mtogo-legacy"
      component       = "legacy"
      use_db          = true
      db_key          = "CONNECTION_STRING_LEGACY"
      use_kafka       = false
      extra_config    = null
      extra_env       = {}
    }
  }
}

# ===========================================
# Deployments
# ===========================================

resource "kubernetes_deployment" "services" {
  for_each = local.services

  metadata {
    name      = each.key
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/name"      = each.key
      "app.kubernetes.io/component" = each.value.component
    }
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = each.key
      }
    }

    template {
      metadata {
        labels = {
          app                      = each.key
          "app.kubernetes.io/name" = each.key
        }
      }

      spec {
        dynamic "image_pull_secrets" {
          for_each = local.use_image_pull_secret ? [1] : []
          content {
            name = var.registry_secret_name
          }
        }

        container {
          name              = each.key
          image             = "${local.image_prefix}/${each.value.image}:${local.image_tag}"
          image_pull_policy = local.use_image_pull_secret ? "Always" : "IfNotPresent"

          port {
            container_port = 8080
            name           = "http"
          }

          # Base environment from configmap
          env_from {
            config_map_ref {
              name = kubernetes_config_map.mtogo.metadata[0].name
            }
          }

          # Extra configmap if specified
          dynamic "env_from" {
            for_each = each.value.extra_config != null ? [1] : []
            content {
              config_map_ref {
                name = each.value.extra_config
              }
            }
          }

          # Database connection if needed
          dynamic "env" {
            for_each = each.value.use_db ? [1] : []
            content {
              name = "ConnectionStrings__DefaultConnection"
              value_from {
                secret_key_ref {
                  name = kubernetes_secret.db_credentials.metadata[0].name
                  key  = each.value.db_key
                }
              }
            }
          }

          # Extra environment variables
          dynamic "env" {
            for_each = each.value.extra_env
            content {
              name  = env.key
              value = env.value
            }
          }

          resources {
            requests = local.default_resources.requests
            limits   = local.default_resources.limits
          }
        }
      }
    }
  }

  depends_on = [
    kubernetes_config_map.gateway,
    kubernetes_secret.db_credentials
  ]
}

# ===========================================
# Services
# ===========================================

resource "kubernetes_service" "services" {
  for_each = local.services

  metadata {
    name      = each.key
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/name" = each.key
    }
  }

  spec {
    type = "ClusterIP"

    port {
      port        = 8080
      target_port = 8080
      name        = "http"
    }

    selector = {
      app = each.key
    }
  }
}
