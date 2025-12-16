# ===========================================
# Ingress Configuration
# ===========================================

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
      "nginx.ingress.kubernetes.io/ssl-redirect"       = "false"
    }
  }

  spec {
    ingress_class_name = "nginx"

    rule {
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
    kubernetes_deployment.services
  ]
}
