# ===========================================
# Demo data seeding (optional)
# ===========================================

locals {
  seed_enabled       = var.seed_demo_data
  seed_script_path   = "${path.module}/../../../sql/seeds/mtogo_demo_seed.sql"
  pg_ssl_mode_lower  = lower(var.postgres_ssl_mode)
  seed_resource_name = "mtogo-demo-seed-${lower(var.environment)}"
}

resource "kubernetes_config_map" "demo_seed" {
  count = local.seed_enabled ? 1 : 0

  metadata {
    name      = local.seed_resource_name
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
      "app.kubernetes.io/name"    = "mtogo-demo-seed"
    }
  }

  data = {
    "mtogo_demo_seed.sql" = file(local.seed_script_path)
  }
}

resource "kubernetes_job" "demo_seed" {
  count = local.seed_enabled ? 1 : 0

  # Don't block terraform apply waiting for the Job to finish.
  # We handle waiting/logging in terraform/deploy.ps1 when -SeedDemoData is used.
  wait_for_completion = false

  metadata {
    name      = local.seed_resource_name
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
      "app.kubernetes.io/name"    = "mtogo-demo-seed"
    }
  }

  spec {
    backoff_limit = 3

    template {
      metadata {
        labels = {
          app = "mtogo-demo-seed"
        }
      }

      spec {
        restart_policy = "Never"

        container {
          name  = "seed"
          image = "postgres:16-alpine"

          env {
            name  = "PGSSLMODE"
            value = local.pg_ssl_mode_lower
          }
          env {
            name  = "PGCONNECT_TIMEOUT"
            value = "5"
          }

          env {
            name = "PGHOST"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.db_credentials.metadata[0].name
                key  = "POSTGRES_HOST"
              }
            }
          }
          env {
            name = "PGUSER"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.db_credentials.metadata[0].name
                key  = "POSTGRES_USER"
              }
            }
          }
          env {
            name = "PGPASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.db_credentials.metadata[0].name
                key  = "POSTGRES_PASSWORD"
              }
            }
          }

          command = [
            "sh",
            "-c",
            "echo 'Waiting for EF-created tables to exist...'; ready=0; for i in $(seq 1 240); do ok=1; psql -d mtogo_partners -tAc \"SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Partners'\" | grep -q 1 || ok=0; psql -d mtogo_agents -tAc \"SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Agents'\" | grep -q 1 || ok=0; psql -d mtogo_legacy -tAc \"SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Customers'\" | grep -q 1 || ok=0; psql -d mtogo_orders -tAc \"SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Orders'\" | grep -q 1 || ok=0; psql -d mtogo_feedback -tAc \"SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='reviews'\" | grep -q 1 || ok=0; if [ \"$ok\" -eq 1 ]; then echo 'Tables are ready. Seeding demo data...'; ready=1; break; fi; echo \"Not ready yet ($i/240). Sleeping 5s...\"; sleep 5; done; if [ \"$ready\" -ne 1 ]; then echo 'Timed out waiting for EF-created tables. Aborting seed job.' >&2; exit 1; fi; psql -v ON_ERROR_STOP=1 -d postgres -f /seed/mtogo_demo_seed.sql; echo 'Seed completed.'"
          ]

          volume_mount {
            name       = "seed"
            mount_path = "/seed"
            read_only  = true
          }
        }

        volume {
          name = "seed"
          config_map {
            name = kubernetes_config_map.demo_seed[0].metadata[0].name
          }
        }
      }
    }
  }

  depends_on = [
    kubernetes_secret.db_credentials,
    kubernetes_config_map.demo_seed
  ]
}
