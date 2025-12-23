# ===========================================
# Ingress Configuration
# ===========================================

locals {
  # Kubernetes Ingress host must be a DNS name (RFC1123). For IP-only access we omit the host
  # match, which makes the ingress apply for any host header.
  ingress_rule_host = can(regex("^\\d{1,3}(\\.\\d{1,3}){3}$", var.ingress_host)) ? "" : var.ingress_host
}

resource "kubernetes_ingress_v1" "mtogo" {
  count = var.install_ingress_controller ? 1 : 0

  metadata {
    name      = "mtogo-ingress"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/name" = "mtogo-ingress"
    }
    annotations = {
      "nginx.ingress.kubernetes.io/proxy-read-timeout" = "3600"
      "nginx.ingress.kubernetes.io/proxy-send-timeout" = "3600"
      "nginx.ingress.kubernetes.io/proxy-http-version" = "1.1"

      # Blazor Server uses SignalR at /_blazor. With multiple replicas, requests must be
      # routed consistently to the same pod (sticky sessions), otherwise the connection
      # ID is not found and the client sees intermittent 404s / failed WebSockets.
      "nginx.ingress.kubernetes.io/affinity"            = "cookie"
      "nginx.ingress.kubernetes.io/session-cookie-name" = "mtogo-affinity"
      "nginx.ingress.kubernetes.io/session-cookie-path" = "/"

      "nginx.ingress.kubernetes.io/ssl-redirect"       = var.ingress_enable_ssl_redirect ? "true" : "false"
      "nginx.ingress.kubernetes.io/force-ssl-redirect" = var.ingress_enable_ssl_redirect ? "true" : "false"

      # HSTS best practice: set at the edge (ingress) so it applies consistently.
      "nginx.ingress.kubernetes.io/hsts"                    = var.ingress_enable_hsts ? "true" : "false"
      "nginx.ingress.kubernetes.io/hsts-max-age"            = tostring(var.ingress_hsts_max_age)
      "nginx.ingress.kubernetes.io/hsts-include-subdomains" = "false"
      "nginx.ingress.kubernetes.io/hsts-preload"            = "false"
    }
  }

  spec {
    ingress_class_name = "nginx"

    tls {
      secret_name = kubernetes_secret.ingress_tls.metadata[0].name
      hosts       = local.ingress_rule_host != "" ? [local.ingress_rule_host] : []
    }

    rule {
      host = local.ingress_rule_host != "" ? local.ingress_rule_host : null
      http {
        # Website - main path
        path {
          path      = "/"
          path_type = "Prefix"
          backend {
            service {
              name = kubernetes_service.services["website"].metadata[0].name
              port {
                number = 8080
              }
            }
          }
        }

        # API Gateway
        path {
          path      = "/api"
          path_type = "Prefix"
          backend {
            service {
              name = kubernetes_service.services["gateway"].metadata[0].name
              port {
                number = 8080
              }
            }
          }
        }

        # Legacy API
        path {
          path      = "/legacy"
          path_type = "Prefix"
          backend {
            service {
              name = kubernetes_service.services["legacy-mtogo"].metadata[0].name
              port {
                number = 8080
              }
            }
          }
        }
      }
    }
  }

  depends_on = [
    helm_release.nginx_ingress,
    kubernetes_secret.ingress_tls,
    kubernetes_deployment.services
  ]
}
