# ===========================================
# Monitoring (Local Kubernetes)
# ===========================================
# This installs Prometheus + Alertmanager in-cluster and exposes them on localhost
# (via LoadBalancer services on Docker Desktop Kubernetes), and installs two
# separate Grafana instances (KPI + SLO) similar to docker-compose.

resource "kubernetes_namespace" "monitoring" {
  count = var.install_monitoring ? 1 : 0

  metadata {
    name = "monitoring"
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
      "environment"               = "dev"
    }
  }
}

locals {
  monitoring_namespace = var.install_monitoring ? try(kubernetes_namespace.monitoring[0].metadata[0].name, "monitoring") : "monitoring"

  grafana_kpi_admin_username = var.grafana_kpi_admin_username
  grafana_kpi_admin_password = var.grafana_kpi_admin_password
  grafana_slo_admin_username = var.grafana_slo_admin_username
  grafana_slo_admin_password = var.grafana_slo_admin_password

  # Helm release/service naming assumptions for prometheus-community/prometheus:
  # - Server service: <release>-server
  # - Alertmanager service: <release>-alertmanager
  prometheus_release_name = "mtogo-prometheus"
  prometheus_server_svc   = "${local.prometheus_release_name}-server"
  prometheus_alert_svc    = "${local.prometheus_release_name}-alertmanager"
  prometheus_server_url   = "http://${local.prometheus_server_svc}.${local.monitoring_namespace}:9090"
  prometheus_alert_url    = "http://${local.prometheus_alert_svc}.${local.monitoring_namespace}:9093"

  discord_webhook_url = var.discord_webhook_url

  # Terraform requires consistent object types in conditionals. We always decode the
  # same template shape, and when no webhook is configured we inject a non-working
  # placeholder URL (matching the docker-compose entrypoint default behavior).
  discord_webhook_url_effective = (
    local.discord_webhook_url != null && trimspace(local.discord_webhook_url) != ""
    ? local.discord_webhook_url
    : "https://discord.com/api/webhooks/not-configured/please-set-DISCORD_WEBHOOK_ALERT"
  )

  alertmanager_config = yamldecode(replace(
    file("${path.module}/../../monitoring/alertmanager/alertmanager.yml.template"),
    "$${DISCORD_WEBHOOK_ALERT}",
    local.discord_webhook_url_effective
  ))

  prometheus_alert_rules = yamldecode(file("${path.module}/../../monitoring/prometheus/alert_rules.yml"))
}

resource "helm_release" "prometheus" {
  count = var.install_monitoring ? 1 : 0

  name             = local.prometheus_release_name
  repository       = "https://prometheus-community.github.io/helm-charts"
  chart            = "prometheus"
  namespace        = local.monitoring_namespace
  create_namespace = false

  values = [
    yamlencode({
      alertmanager = {
        enabled = true
        service = {
          type        = "LoadBalancer"
          servicePort = 9093
        }
      }

      # Configure Alertmanager routing (Discord) using the same config template as docker-compose.
      alertmanagerFiles = {
        "alertmanager.yml" = local.alertmanager_config
      }

      # Disable optional components for local dev (reduces noise and avoids
      # privileged/host access requirements on Docker Desktop Kubernetes).
      "prometheus-node-exporter" = {
        enabled = false
      }
      "kube-state-metrics" = {
        enabled = false
      }
      "prometheus-pushgateway" = {
        enabled = false
      }

      pushgateway = {
        enabled = false
      }
      kubeStateMetrics = {
        enabled = false
      }
      nodeExporter = {
        enabled = false
      }
      server = {
        service = {
          type        = "LoadBalancer"
          servicePort = 9090
        }
        serverFiles = {
          "prometheus.yml" = {
            global = {
              scrape_interval     = "15s"
              evaluation_interval = "15s"
            }

            alerting = {
              alertmanagers = [
                {
                  static_configs = [
                    { targets = ["${local.prometheus_alert_svc}.${local.monitoring_namespace}:9093"] }
                  ]
                }
              ]
            }

            rule_files = [
              "alert_rules.yml"
            ]

            scrape_configs = [
              {
                job_name = "prometheus"
                static_configs = [
                  { targets = ["localhost:9090"] }
                ]
              },
              {
                job_name        = "order-service"
                metrics_path    = "/metrics"
                scrape_interval = "15s"
                static_configs = [
                  { targets = ["order-service.mtogo.svc.cluster.local:8080"] }
                ]
              },
              {
                job_name        = "partner-service"
                metrics_path    = "/metrics"
                scrape_interval = "15s"
                static_configs = [
                  { targets = ["partner-service.mtogo.svc.cluster.local:8080"] }
                ]
              },
              {
                job_name        = "agent-service"
                metrics_path    = "/metrics"
                scrape_interval = "15s"
                static_configs = [
                  { targets = ["agent-service.mtogo.svc.cluster.local:8080"] }
                ]
              },
              {
                job_name        = "gateway"
                metrics_path    = "/metrics"
                scrape_interval = "15s"
                static_configs = [
                  { targets = ["gateway.mtogo.svc.cluster.local:8080"] }
                ]
              }
            ]
          }

          # Business KPI alert rules shared with docker-compose.
          "alert_rules.yml" = local.prometheus_alert_rules
        }
      }
    })
  ]

  depends_on = [module.mtogo_app]
}

resource "kubernetes_config_map" "grafana_kpi_datasources" {
  count = var.install_monitoring ? 1 : 0

  metadata {
    name      = "grafana-kpi-datasources"
    namespace = local.monitoring_namespace
    labels = {
      grafana_datasource_kpi = "1"
    }
  }

  data = {
    "datasources.yml" = yamlencode({
      apiVersion = 1
      datasources = [
        {
          name      = "Prometheus"
          type      = "prometheus"
          access    = "proxy"
          url       = local.prometheus_server_url
          isDefault = true
          uid       = "prometheus"
          editable  = false
          jsonData = {
            httpMethod      = "POST"
            manageAlerts    = true
            prometheusType  = "Prometheus"
            alertmanagerUid = "alertmanager"
          }
        },
        {
          name     = "Alertmanager"
          type     = "alertmanager"
          access   = "proxy"
          url      = local.prometheus_alert_url
          uid      = "alertmanager"
          editable = false
          jsonData = {
            implementation = "prometheus"
          }
        }
      ]
    })
  }

  depends_on = [helm_release.prometheus]
}

resource "kubernetes_config_map" "grafana_slo_datasources" {
  count = var.install_monitoring ? 1 : 0

  metadata {
    name      = "grafana-slo-datasources"
    namespace = local.monitoring_namespace
    labels = {
      grafana_datasource_slo = "1"
    }
  }

  data = {
    "datasources.yml" = yamlencode({
      apiVersion = 1
      datasources = [
        {
          name      = "Prometheus"
          type      = "prometheus"
          access    = "proxy"
          url       = local.prometheus_server_url
          isDefault = true
          uid       = "prometheus"
          editable  = false
          jsonData = {
            httpMethod     = "POST"
            prometheusType = "Prometheus"
          }
        }
      ]
    })
  }

  depends_on = [helm_release.prometheus]
}

resource "kubernetes_config_map" "grafana_kpi_dashboards" {
  count = var.install_monitoring ? 1 : 0

  metadata {
    name      = "grafana-kpi-dashboards"
    namespace = local.monitoring_namespace
    labels = {
      grafana_dashboard_kpi = "1"
    }
  }

  data = {
    "mtogo-kpi-dashboard.json" = file("${path.module}/../../monitoring/grafana/dashboards/mtogo-kpi-dashboard.json")
  }
}

resource "kubernetes_config_map" "grafana_slo_dashboards" {
  count = var.install_monitoring ? 1 : 0

  metadata {
    name      = "grafana-slo-dashboards"
    namespace = local.monitoring_namespace
    labels = {
      grafana_dashboard_slo = "1"
    }
  }

  data = {
    "mtogo-slo-dashboard.json" = file("${path.module}/../../monitoring-infrastructure/grafana/dashboards/mtogo-slo-dashboard.json")
  }
}

resource "helm_release" "grafana_kpi" {
  count = var.install_monitoring ? 1 : 0

  name             = "grafana-kpi"
  repository       = "https://grafana.github.io/helm-charts"
  chart            = "grafana"
  namespace        = local.monitoring_namespace
  create_namespace = false

  values = [
    yamlencode({
      adminUser     = local.grafana_kpi_admin_username
      adminPassword = local.grafana_kpi_admin_password
      service = {
        type       = "LoadBalancer"
        port       = 3000
        targetPort = 3000
      }
      sidecar = {
        dashboards = {
          enabled = true
          label   = "grafana_dashboard_kpi"
        }
        datasources = {
          enabled = true
          label   = "grafana_datasource_kpi"
        }
      }
    })
  ]

  depends_on = [
    helm_release.prometheus,
    kubernetes_config_map.grafana_kpi_datasources,
    kubernetes_config_map.grafana_kpi_dashboards,
  ]
}

resource "helm_release" "grafana_slo" {
  count = var.install_monitoring ? 1 : 0

  name             = "grafana-slo"
  repository       = "https://grafana.github.io/helm-charts"
  chart            = "grafana"
  namespace        = local.monitoring_namespace
  create_namespace = false

  values = [
    yamlencode({
      adminUser     = local.grafana_slo_admin_username
      adminPassword = local.grafana_slo_admin_password
      service = {
        type = "LoadBalancer"
        # Expose on 3001 (like docker-compose), while Grafana listens on 3000 internally.
        port       = 3001
        targetPort = 3000
      }
      sidecar = {
        dashboards = {
          enabled = true
          label   = "grafana_dashboard_slo"
        }
        datasources = {
          enabled = true
          label   = "grafana_datasource_slo"
        }
      }
    })
  ]

  depends_on = [
    helm_release.prometheus,
    kubernetes_config_map.grafana_slo_datasources,
    kubernetes_config_map.grafana_slo_dashboards,
  ]
}
