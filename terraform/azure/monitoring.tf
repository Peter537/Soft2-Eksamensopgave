# ===========================================
# Monitoring (AKS - in-cluster Prometheus + Alertmanager)
# ===========================================
# Azure setup uses Azure Monitor Workspace + Azure Managed Grafana for dashboards.
# This file adds optional in-cluster Prometheus + Alertmanager so we can reuse the
# same Alertmanager config template as docker-compose (Discord routing).

resource "kubernetes_namespace" "monitoring" {
  count = var.install_monitoring ? 1 : 0

  metadata {
    name = "monitoring"
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
      "environment"               = var.environment
    }
  }
}

locals {
  monitoring_namespace = var.install_monitoring ? try(kubernetes_namespace.monitoring[0].metadata[0].name, "monitoring") : "monitoring"

  # Helm release/service naming assumptions for prometheus-community/prometheus:
  # - Server service: <release>-server
  # - Alertmanager service: <release>-alertmanager
  prometheus_release_name = "mtogo-prometheus"
  prometheus_alert_svc    = "${local.prometheus_release_name}-alertmanager"

  discord_webhook_url = var.discord_webhook_url

  # Keep behavior consistent with docker-compose entrypoint.sh: when no webhook is set,
  # inject a non-working placeholder URL.
  discord_webhook_url_effective = (
    trimspace(coalesce(local.discord_webhook_url, "")) != ""
    ? local.discord_webhook_url
    : "https://discord.com/api/webhooks/not-configured/please-set-DISCORD_WEBHOOK_ALERT"
  )

  # Render the exact same template used by docker-compose.
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
          type        = "ClusterIP"
          servicePort = 9093
        }

        # Configure Alertmanager routing (Discord) using the same config template as docker-compose.
        config = local.alertmanager_config
      }

      # Keep the in-cluster Prometheus lightweight.
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
          type        = "ClusterIP"
          servicePort = 9090
        }
      }

      # Prometheus chart reads configuration from top-level serverFiles.
      serverFiles = {
        "alerting_rules.yml" = local.prometheus_alert_rules
      }
    })
  ]

  depends_on = [module.mtogo_app]
}