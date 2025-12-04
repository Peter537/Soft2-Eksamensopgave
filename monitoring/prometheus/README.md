# Prometheus

Prometheus indsamler og lagrer metrics fra alle MToGo microservices.

## Formål

Prometheus bruges til at:

- **Indsamle Metrics**: Scrape `/metrics` endpoints fra alle services
- **Lagre Tidsserie-data**: Time-series database for historisk analyse
- **Evaluere Alert-regler**: Trigger alerts baseret på definerede regler

## Filer

| Fil                                | Beskrivelse                           |
| ---------------------------------- | ------------------------------------- |
| [prometheus.yml](prometheus.yml)   | Hovedkonfiguration med scrape targets |
| [alert_rules.yml](alert_rules.yml) | Business intelligence alert-regler    |

## Scrape Targets

Prometheus scraper følgende services:

| Job Name          | Target               | Beskrivelse                |
| ----------------- | -------------------- | -------------------------- |
| `prometheus`      | localhost:9090       | Prometheus self-monitoring |
| `order-service`   | order-service:8080   | Ordre-metrics og KPI'er    |
| `partner-service` | partner-service:8080 | Partner-metrics            |
| `agent-service`   | agent-service:8080   | Agent-metrics              |
| `gateway`         | gateway:8080         | HTTP request metrics       |

## Alert-regler

Alert-reglerne er opdelt i kategorier:

### Customer Alerts

| Alert                          | Severity | Beskrivelse               |
| ------------------------------ | -------- | ------------------------- |
| `ActiveCustomersDecline`       | warning  | Aktive kunder faldet ≥10% |
| `ActiveCustomersGrowth`        | info     | Aktive kunder steget ≥20% |
| `ActiveCustomersCriticallyLow` | warning  | Færre end 5 aktive kunder |

### Order Alerts

| Alert                | Severity | Beskrivelse                  |
| -------------------- | -------- | ---------------------------- |
| `DailyOrdersDecline` | warning  | Daglige ordrer faldet ≥15%   |
| `DailyOrdersSurge`   | info     | Daglige ordrer steget ≥30%   |
| `NoRecentOrders`     | warning  | Ingen ordrer den sidste time |

### Partner Alerts

| Alert               | Severity | Beskrivelse                 |
| ------------------- | -------- | --------------------------- |
| `NoActivePartners`  | critical | Ingen aktive partnere       |
| `LowActivePartners` | warning  | Færre end 5 aktive partnere |
| `PartnerGrowth`     | info     | +3 nye partnere denne uge   |

### Agent Alerts

| Alert                   | Severity | Beskrivelse                |
| ----------------------- | -------- | -------------------------- |
| `NoActiveAgents`        | critical | Ingen aktive agenter       |
| `LowActiveAgents`       | warning  | Færre end 3 aktive agenter |
| `HighAgentAvailability` | info     | 10+ agenter tilgængelige   |

## MToGo Metrics

Custom metrics eksponeret af microservices:

| Metric                                  | Type    | Beskrivelse                 |
| --------------------------------------- | ------- | --------------------------- |
| `mtogo_active_customers_monthly`        | Gauge   | Aktive kunder denne måned   |
| `mtogo_active_customers_previous_month` | Gauge   | Aktive kunder forrige måned |
| `mtogo_orders_total`                    | Counter | Totalt antal ordrer         |
| `mtogo_orders_last_day`                 | Gauge   | Ordrer de seneste 24 timer  |
| `mtogo_orders_same_day_last_week`       | Gauge   | Ordrer samme dag sidste uge |
| `mtogo_orders_last_hour`                | Gauge   | Ordrer den seneste time     |
| `mtogo_active_partners`                 | Gauge   | Antal aktive partnere       |
| `mtogo_active_agents`                   | Gauge   | Antal aktive agenter        |
