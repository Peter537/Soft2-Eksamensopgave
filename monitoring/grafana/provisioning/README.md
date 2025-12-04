# Grafana Provisioning

Automatisk opsætning af Grafana datasources og dashboards ved containerstart.

## Formål

Provisioning sikrer at Grafana altid starter med:

- **Korrekte Datasources**: Prometheus og Alertmanager forbundet
- **Dashboards**: Alle KPI-dashboards tilgængelige

## Mappestruktur

| Mappe                         | Beskrivelse                              |
| ----------------------------- | ---------------------------------------- |
| [dashboards/](./dashboards)   | Konfiguration til dashboard-provisioning |
| [datasources/](./datasources) | Konfiguration af datakilde-forbindelser  |

## Datasources

Defineret i [datasources/datasources.yml](./datasources/datasources.yml):

| Datasource   | Type         | URL                      | Standard |
| ------------ | ------------ | ------------------------ | -------- |
| Prometheus   | prometheus   | http://prometheus:9090   | Ja       |
| Alertmanager | alertmanager | http://alertmanager:9093 | Nej      |

## Dashboards

Defineret i [dashboards/dashboards.yml](./dashboards/dashboards.yml):

- **Kilde**: `/var/lib/grafana/dashboards`
- **Mappe i Grafana**: MToGo
- **Opdateringsinterval**: 30 sekunder
- **UI-ændringer tilladt**: Ja

## Sådan fungerer det

1. Grafana starter og læser provisioning-konfigurationen
2. Datasources oprettes baseret på `datasources.yml`
3. Dashboards indlæses fra den konfigurerede sti
4. Dashboards opdateres automatisk hvis JSON-filerne ændres
