# Monitoring

Komplet monitoring-stack til MToGo-platformen baseret på Prometheus, Grafana og Alertmanager.

## Formål

Monitoring-stacken giver fuld observerbarhed af MToGo-platformen:

- **Metrics-indsamling**: Indsaml metrics fra alle microservices
- **Visualisering**: Dashboards til KPI-overvågning
- **Alerting**: Automatiske notifikationer

## Teknologi Stack

- **Prometheus**: Metrics-indsamling og -lagring
- **Grafana**: Dashboard-visualisering
- **Alertmanager**: Alert-routing og notifikationer

## Mappestruktur

| Mappe                           | Beskrivelse                              |
| ------------------------------- | ---------------------------------------- |
| [alertmanager](./alertmanager/) | Alertmanager-konfiguration og templates  |
| [grafana](./grafana/)           | Dashboards og Grafana-provisioning       |
| [prometheus](./prometheus/)     | Prometheus-konfiguration og alert-regler |

## Adgang

| Service       | URL                   |
| ------------- | --------------------- |
| Grafana       | http://localhost:3000 |
| Grafana (SLO) | http://localhost:3001 |
| Prometheus    | http://localhost:9090 |
| Alertmanager  | http://localhost:9093 |

Bemærk: Grafana (SLO) er en separat instans med egne dashboards i [monitoring-infrastructure/](../monitoring-infrastructure/).

## KPI Dashboards

Grafana-dashboards overvåger følgende KPI'er:

- **Aktive Kunder**: Månedligt aktive kunder
- **Ordremængde**: Ordrer per time/dag
- **Aktive Partnere**: Antal aktive restauranter
- **Aktive Agenter**: Antal aktive leveringsagenter

## Alerts

### Kategori

Alle alerts tilhører kategorien `business_kpi` (forretnings-KPI overvågning).

### Severity Levels

| Severity   | Beskrivelse                      |
| ---------- | -------------------------------- |
| `info`     | Positive trends og milepæle      |
| `warning`  | Negative trends - bør undersøges |
| `critical` | Kræver øjeblikkelig handling     |

### KPI Alerts

| KPI                | Info Alert            | Warning Alert          | Critical Alert   |
| ------------------ | --------------------- | ---------------------- | ---------------- |
| `active_customers` | ActiveCustomersGrowth | ActiveCustomersDecline | -                |
| `orders_per_day`   | DailyOrdersSurge      | DailyOrdersDecline     | -                |
| `orders_per_hour`  | -                     | NoRecentOrders         | -                |
| `active_partners`  | PartnerGrowth         | LowActivePartners      | NoActivePartners |
| `active_agents`    | HighAgentAvailability | LowActiveAgents        | NoActiveAgents   |
