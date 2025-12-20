# Monitoring Infrastructure (SLO)

Denne mappe indeholder en separat monitoring-stack til **SLO-overvågning**.

Formål:

- Køre en **anden Grafana-instans** end KPI-Grafanaen (i `/monitoring`).
- Visualisere SLO'er for Order Service.

## Lokal adgang

- SLO Grafana: http://localhost:3001

## Struktur

- `grafana/`:
  - `provisioning/`: Grafana provisioning (datasources + dashboards)
  - `dashboards/`: JSON dashboards (auto-loaded)

## Datasource

Lokal setup bruger samme Prometheus som nuværende setup (fra `docker-compose.yml`).
