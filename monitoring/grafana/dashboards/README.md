# Grafana Dashboards

JSON-filer der definerer Grafana-dashboards til MToGo-platformen.

## Dashboards

| Fil                                                    | Beskrivelse                   |
| ------------------------------------------------------ | ----------------------------- |
| [mtogo-kpi-dashboard.json](./mtogo-kpi-dashboard.json) | Hoved KPI-dashboard for MToGo |

## MToGo KPI Dashboard

Dashboardet viser følgende paneler:

### Key Performance Indicators

- **Aktive Kunder (Måned)**: Antal kunder med mindst én ordre de seneste 30 dage
- **Ordrer per Time**: Antal ordrer placeret den seneste time
- **Aktive Partnere**: Antal partnere aktiveret til at modtage ordrer
- **Aktive Agenter**: Antal agenter tilgængelige til levering

### Trend Comparison

- **Kunde Trend**: Procentvis ændring i aktive kunder vs. forrige måned
- **Ordre Trend**: Procentvis ændring i ordrer vs. samme dag sidste uge
- **Kunder (Forrige Måned)**: Reference-værdi for sammenligning
- **Ordrer (Samme Dag Sidste Uge)**: Reference-værdi for sammenligning

### Order Volumes

- **Ordrer per Dag**: Antal ordrer de seneste 24 timer
- **Totalt Oprettede Ordrer**: Samlet antal ordrer siden servicestart
- **Order Rate (5m average)**: Graf over ordrer per minut

### Resource Overview

- **Platform Activity Overview**: Tidsserie over kunder, partnere og agenter
- **Order Volume Trends**: Tidsserie over ordrer per time og dag

## Provisioning

Dashboards indlæses automatisk ved Grafana-opstart via [provisioning/dashboards/dashboards.yml](../provisioning/dashboards/dashboards.yml).
