# ===========================================
# Local Infrastructure (PostgreSQL + Kafka)
# ===========================================
# Only deployed when deploy_postgres and deploy_kafka are true
# Used for local Kubernetes deployments

# ===========================================
# PostgreSQL (Local Only)
# ===========================================

resource "kubernetes_persistent_volume_claim" "postgres" {
  count = var.deploy_postgres ? 1 : 0

  metadata {
    name      = "mtogo-db-pvc"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
  }

  spec {
    access_modes = ["ReadWriteOnce"]
    resources {
      requests = {
        storage = "5Gi"
      }
    }
  }
}

resource "kubernetes_deployment" "postgres" {
  count = var.deploy_postgres ? 1 : 0

  metadata {
    name      = "mtogo-db"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/name"      = "mtogo-db"
      "app.kubernetes.io/component" = "database"
    }
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = "mtogo-db"
      }
    }

    strategy {
      type = "Recreate"
    }

    template {
      metadata {
        labels = {
          app                      = "mtogo-db"
          "app.kubernetes.io/name" = "mtogo-db"
        }
      }

      spec {
        container {
          name  = "postgres"
          image = "postgres:16-alpine"

          port {
            container_port = 5432
            name           = "postgres"
          }

          env {
            name  = "POSTGRES_USER"
            value = var.postgres_admin_username
          }

          env {
            name  = "POSTGRES_PASSWORD"
            value = var.postgres_admin_password
          }

          env {
            name  = "POSTGRES_DB"
            value = "mtogo"
          }

          volume_mount {
            name       = "postgres-data"
            mount_path = "/var/lib/postgresql/data"
          }

          resources {
            requests = {
              memory = "256Mi"
              cpu    = "250m"
            }
            limits = {
              memory = "512Mi"
              cpu    = "500m"
            }
          }

          liveness_probe {
            exec {
              command = ["pg_isready", "-U", var.postgres_admin_username, "-d", "mtogo"]
            }
            initial_delay_seconds = 30
            period_seconds        = 10
          }

          readiness_probe {
            exec {
              command = ["pg_isready", "-U", var.postgres_admin_username, "-d", "mtogo"]
            }
            initial_delay_seconds = 5
            period_seconds        = 5
          }
        }

        volume {
          name = "postgres-data"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim.postgres[0].metadata[0].name
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "postgres" {
  count = var.deploy_postgres ? 1 : 0

  metadata {
    name      = "mtogo-db"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
  }

  spec {
    type = "ClusterIP"

    port {
      port        = 5432
      target_port = 5432
      name        = "postgres"
    }

    selector = {
      app = "mtogo-db"
    }
  }
}

# ===========================================
# Kafka (Optional - can be deployed in cluster)
# ===========================================

resource "kubernetes_deployment" "kafka" {
  count = var.deploy_kafka ? 1 : 0

  metadata {
    name      = "kafka"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/name"      = "kafka"
      "app.kubernetes.io/component" = "messaging"
    }
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = "kafka"
      }
    }

    template {
      metadata {
        labels = {
          app                      = "kafka"
          "app.kubernetes.io/name" = "kafka"
        }
      }

      spec {
        container {
          name  = "kafka"
          image = "apache/kafka:3.7.0"

          port {
            container_port = 9092
            name           = "kafka"
          }

          env {
            name  = "KAFKA_NODE_ID"
            value = "1"
          }
          env {
            name  = "KAFKA_PROCESS_ROLES"
            value = "broker,controller"
          }
          env {
            name  = "KAFKA_CONTROLLER_QUORUM_VOTERS"
            value = "1@localhost:9093"
          }
          env {
            name  = "KAFKA_CONTROLLER_LISTENER_NAMES"
            value = "CONTROLLER"
          }
          env {
            name  = "KAFKA_LISTENERS"
            value = "PLAINTEXT://:9092,CONTROLLER://:9093"
          }
          env {
            name  = "KAFKA_ADVERTISED_LISTENERS"
            value = "PLAINTEXT://kafka:9092"
          }
          env {
            name  = "KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"
            value = "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT"
          }
          env {
            name  = "KAFKA_INTER_BROKER_LISTENER_NAME"
            value = "PLAINTEXT"
          }
          env {
            name  = "KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR"
            value = "1"
          }
          env {
            name  = "KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR"
            value = "1"
          }
          env {
            name  = "KAFKA_TRANSACTION_STATE_LOG_MIN_ISR"
            value = "1"
          }
          env {
            name  = "KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS"
            value = "0"
          }

          resources {
            requests = {
              memory = "512Mi"
              cpu    = "250m"
            }
            limits = {
              memory = "1Gi"
              cpu    = "500m"
            }
          }

          readiness_probe {
            tcp_socket {
              port = 9092
            }
            initial_delay_seconds = 30
            period_seconds        = 10
          }

          liveness_probe {
            tcp_socket {
              port = 9092
            }
            initial_delay_seconds = 60
            period_seconds        = 20
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "kafka" {
  count = var.deploy_kafka ? 1 : 0

  metadata {
    name      = "kafka"
    namespace = kubernetes_namespace.mtogo.metadata[0].name
  }

  spec {
    type = "ClusterIP"

    port {
      port        = 9092
      target_port = 9092
      name        = "kafka"
    }

    selector = {
      app = "kafka"
    }
  }
}
