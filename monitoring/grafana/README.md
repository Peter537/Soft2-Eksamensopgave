# Grafana

Grafana leverer dashboards til visualisering af MToGo-platformens metrics og KPI'er.

## Formål

Grafana bruges til at:

- **Visualisere**: Vise realtids metrics fra Prometheus
- **Overvåge KPI'er**: Spore forretningskritiske nøgletal
- **Alerting**: Vise aktive alerts fra Alertmanager

## Mappestruktur

| Mappe                           | Beskrivelse                                       |
| ------------------------------- | ------------------------------------------------- |
| [dashboards/](./dashboards)     | JSON-filer med dashboard-definitioner             |
| [provisioning/](./provisioning) | Automatisk opsætning af datasources og dashboards |

## Dashboards

### MToGo KPI Dashboard

Hoveddasboardet ([mtogo-kpi-dashboard.json](./dashboards/mtogo-kpi-dashboard.json)) viser:

- **Aktive Kunder**: Månedligt aktive kunder med trend
- **Ordremængde**: Ordrer per time og dag
- **Aktive Partnere**: Antal aktive restauranter
- **Aktive Agenter**: Antal aktive leveringsagenter
- **Systemstatus**: Helbred for alle microservices

## Provisioning

Grafana konfigureres automatisk ved opstart:

### Datasources

Defineret i [provisioning/datasources/datasources.yml](./provisioning/datasources/datasources.yml):

- **Prometheus**: Primær datakilde for metrics
- **Alertmanager**: Viser aktive alerts

### Dashboards

Dashboards indlæses automatisk fra `/var/lib/grafana/dashboards`, som her er i [dashboards](./dashboards).

## Adgang

URL: http://localhost:3000
