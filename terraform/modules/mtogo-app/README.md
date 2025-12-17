# mtogo-app (Terraform Module)

Kubernetes-modul der deployer MToGo-platformens services til en Kubernetes cluster.

Modulet kan bruges både til **local** og **cloud** deployments, fordi det kun opretter Kubernetes resources (og evt. lokal in-cluster infrastruktur).

## Hvad modulet opretter

- Namespace (`mtogo` som default)
- (Valgfrit) Image pull secret til container registry
- (Valgfrit) NGINX Ingress Controller (via Helm) + Ingress routes
- ConfigMaps og Secrets til app-konfiguration
- Deployments + Services for MToGo services
- (Valgfrit) Lokal infrastruktur i clusteren:
  - PostgreSQL (med init af service-databaser)
  - Kafka

## Ingress routes

Når ingress er aktiveret, routes trafikken typisk sådan:

- `/` -> Website
- `/api` -> Gateway
- `/legacy` -> LegacyMToGo

## Centrale input variables

Variablerne er defineret i [variables.tf](./variables.tf).

Typiske værdier:

- `environment` (dev/staging/prod)
- `namespace`
- `image_registry` + `image_tag`
- `postgres_host`, `postgres_admin_username`, `postgres_admin_password`, `postgres_ssl_mode`
- Registry auth (valgfrit): `registry_secret_name`, `registry_server`, `registry_username`, `registry_password`
- `install_ingress_controller`
- `deploy_postgres`, `deploy_kafka`, `kafka_bootstrap_servers`
- Seeded ManagementService admin: `management_username`, `management_password`, `management_name`

## Outputs

Se [outputs.tf](./outputs.tf) for aktuelle outputs (namespace, services map, ingress name).

## Filer

| Fil                                      | Beskrivelse                                        |
| ---------------------------------------- | -------------------------------------------------- |
| [main.tf](./main.tf)                     | Namespace, secrets, configmaps, ingress controller |
| [services.tf](./services.tf)             | Deployments + services for MToGo microservices     |
| [ingress.tf](./ingress.tf)               | Ingress routes (website/api/legacy)                |
| [infrastructure.tf](./infrastructure.tf) | Lokal in-cluster PostgreSQL + Kafka (valgfrit)     |
| [variables.tf](./variables.tf)           | Input variables                                    |
| [outputs.tf](./outputs.tf)               | Outputs                                            |
