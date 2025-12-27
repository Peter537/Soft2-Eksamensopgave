# ===========================================
# Resource Group
# ===========================================

locals {
  location_slug = replace(lower(var.location), " ", "")
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = var.location

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
  }
}

# ===========================================
# Azure Kubernetes Service (AKS)
# ===========================================

resource "azurerm_kubernetes_cluster" "main" {
  name                = "aks-${var.project_name}-${var.environment}-${local.location_slug}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = "${var.project_name}-${var.environment}-${local.location_slug}"
  kubernetes_version  = var.kubernetes_version

  # Azure Monitor (Managed Prometheus)
  # Exposes Prometheus metrics in Azure Monitor Workspace.
  monitor_metrics {
    annotations_allowed = null
    labels_allowed      = null
  }

  default_node_pool {
    name                 = "default"
    vm_size              = var.node_vm_size
    auto_scaling_enabled = var.enable_auto_scaling

    # Node count settings
    node_count = var.enable_auto_scaling ? null : var.node_count
    min_count  = var.enable_auto_scaling ? var.node_count : null
    max_count  = var.enable_auto_scaling ? var.node_count_max : null

    # Enable temporary disk for OS
    os_disk_type    = "Managed"
    os_disk_size_gb = 30
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin    = "azure"
    load_balancer_sku = "standard"
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
  }
}

# ===========================================
# Static Public IP for ingress-nginx
# ===========================================
# We do not use DNS. To run HTTPS on a stable IP, we must reserve a public IP and
# assign it to the ingress-nginx LoadBalancer service.
#
# IMPORTANT:
# Azure Kubernetes LoadBalancer services can only use a pre-created Public IP if
# it exists in the AKS *node resource group*.

resource "azurerm_public_ip" "ingress" {
  name                = "pip-${var.project_name}-${var.environment}-ingress"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_kubernetes_cluster.main.node_resource_group

  allocation_method = "Static"
  sku               = "Standard"

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
    Purpose     = "ingress-nginx"
  }
}

# ===========================================
# Azure Monitor Workspace (Managed Prometheus storage)
# ===========================================

resource "azurerm_monitor_workspace" "slo" {
  name                = "amw-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
    Purpose     = "slo-monitoring"
  }
}

# ===========================================
# Azure Managed Grafana (Separate SLO instance)
# ===========================================

resource "azurerm_dashboard_grafana" "slo" {
  name                  = "grafana-slo-${var.project_name}-${var.environment}"
  resource_group_name   = azurerm_resource_group.main.name
  location              = azurerm_resource_group.main.location
  grafana_major_version = 11

  # Keep it reachable for demos/assessment.
  public_network_access_enabled = true
  api_key_enabled               = true

  identity {
    type = "SystemAssigned"
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
    Purpose     = "slo-visualization"
  }
}

# ===========================================
# Azure Managed Grafana (Separate KPI instance)
# ===========================================

resource "azurerm_dashboard_grafana" "kpi" {
  name                  = "grafana-kpi-${var.project_name}-${var.environment}"
  resource_group_name   = azurerm_resource_group.main.name
  location              = azurerm_resource_group.main.location
  grafana_major_version = 11

  # Keep it reachable for demos/assessment.
  public_network_access_enabled = true
  api_key_enabled               = true

  identity {
    type = "SystemAssigned"
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
    Purpose     = "kpi-visualization"
  }
}

# ===========================================
# Azure Monitor Workspace RBAC for Grafana MSI
# ===========================================
# We create the Prometheus data source via deploy.ps1 using Grafana's system-assigned
# managed identity (MSI). Without the built-in workspace integration, we must grant
# the MSI rights to query the Azure Monitor Workspace.

resource "azurerm_role_assignment" "grafana_slo_amw_monitoring_data_reader" {
  scope                = azurerm_monitor_workspace.slo.id
  role_definition_name = "Monitoring Data Reader"
  principal_id         = azurerm_dashboard_grafana.slo.identity[0].principal_id
}

resource "azurerm_role_assignment" "grafana_kpi_amw_monitoring_data_reader" {
  scope                = azurerm_monitor_workspace.slo.id
  role_definition_name = "Monitoring Data Reader"
  principal_id         = azurerm_dashboard_grafana.kpi.identity[0].principal_id
}

# ===========================================
# Azure Managed Grafana RBAC (separate audiences)
# ===========================================
# Azure Managed Grafana uses Entra ID sign-in. “Two different logins” here means
# assigning different Entra ID users/groups to the KPI vs SLO Grafana instances.

resource "azurerm_role_assignment" "grafana_slo_admin" {
  for_each = toset(var.grafana_slo_admin_principal_ids)

  scope                = azurerm_dashboard_grafana.slo.id
  role_definition_name = "Grafana Admin"
  principal_id         = each.value
}

locals {
  grafana_kpi_admin_principal_ids_effective = length(var.grafana_kpi_admin_principal_ids) > 0 ? var.grafana_kpi_admin_principal_ids : var.grafana_slo_admin_principal_ids
}

resource "azurerm_role_assignment" "grafana_kpi_admin" {
  for_each = toset(local.grafana_kpi_admin_principal_ids_effective)

  scope                = azurerm_dashboard_grafana.kpi.id
  role_definition_name = "Grafana Admin"
  principal_id         = each.value
}

resource "azurerm_role_assignment" "grafana_kpi_viewer" {
  for_each = toset(var.grafana_kpi_viewer_principal_ids)

  scope                = azurerm_dashboard_grafana.kpi.id
  role_definition_name = "Grafana Viewer"
  principal_id         = each.value
}

resource "azurerm_role_assignment" "grafana_kpi_editor" {
  for_each = toset(var.grafana_kpi_editor_principal_ids)

  scope                = azurerm_dashboard_grafana.kpi.id
  role_definition_name = "Grafana Editor"
  principal_id         = each.value
}

# ===========================================
# Azure Database for PostgreSQL Flexible Server
# ===========================================

resource "azurerm_postgresql_flexible_server" "main" {
  name                   = "psql-${var.project_name}-${var.environment}"
  resource_group_name    = azurerm_resource_group.main.name
  location               = azurerm_resource_group.main.location
  version                = "16"
  administrator_login    = var.postgres_admin_username
  administrator_password = var.postgres_admin_password
  zone                   = "1"
  storage_mb             = 32768
  sku_name               = "B_Standard_B1ms"

  tags = {
    Environment = var.environment
    Project     = var.project_name
    ManagedBy   = "terraform"
  }
}

# Allow Azure services to access PostgreSQL
resource "azurerm_postgresql_flexible_server_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_postgresql_flexible_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Create databases for each service
resource "azurerm_postgresql_flexible_server_database" "orders" {
  name      = "mtogo_orders"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "agents" {
  name      = "mtogo_agents"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "feedback" {
  name      = "mtogo_feedback"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "partners" {
  name      = "mtogo_partners"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "legacy" {
  name      = "mtogo_legacy"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "management" {
  name      = "mtogo_management"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}

resource "azurerm_postgresql_flexible_server_database" "logs" {
  name      = "mtogo_logs"
  server_id = azurerm_postgresql_flexible_server.main.id
  collation = "en_US.utf8"
  charset   = "UTF8"
}
