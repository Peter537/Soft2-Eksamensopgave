# MToGo Deployment Guide

This document explains the different deployment options available for the MToGo platform and how to use them.

## Table of Contents

1. [Overview](#overview)
2. [Terraform Module Architecture](#terraform-module-architecture)
3. [Local Development with Docker Compose](#local-development-with-docker-compose)
4. [Local Kubernetes with Terraform](#local-kubernetes-with-terraform)
5. [Azure Production Deployment](#azure-production-deployment)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Switching Cloud Providers](#switching-cloud-providers)
8. [Prerequisites & Setup](#prerequisites--setup)
9. [Troubleshooting](#troubleshooting)

---

## Overview

MToGo supports multiple deployment strategies using a **modular Terraform architecture**:

| Method              | Use Case            | Infrastructure          | Complexity |
| ------------------- | ------------------- | ----------------------- | ---------- |
| **Docker Compose**  | Quick local dev     | Local Docker            | Simple     |
| **Terraform Local** | Local K8s testing   | Docker Desktop/Minikube | Medium     |
| **Terraform Azure** | Production          | Azure AKS               | Advanced   |
| **Terraform AWS**   | Production (future) | Amazon EKS              | Advanced   |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              MToGo Platform                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────┐    ┌─────────────────────────────────────────────┐    │
│  │   Website   │───▶│                 Gateway                     │    │
│  │  (Blazor)   │    │              (YARP Proxy)                   │    │
│  └─────────────┘    └─────────────┬───────────────────────────────┘    │
│                                   │                                     │
│         ┌─────────────────────────┼─────────────────────────────┐      │
│         ▼                         ▼                             ▼      │
│  ┌─────────────┐          ┌─────────────┐              ┌─────────────┐ │
│  │   Order     │          │   Partner   │              │   Agent     │ │
│  │  Service    │          │   Service   │              │  Service    │ │
│  └──────┬──────┘          └──────┬──────┘              └──────┬──────┘ │
│         │                        │                            │        │
│         └────────────────────────┼────────────────────────────┘        │
│                                  ▼                                      │
│                           ┌─────────────┐                               │
│                           │   Kafka     │                               │
│                           │  (Events)   │                               │
│                           └──────┬──────┘                               │
│                                  │                                      │
│         ┌────────────────────────┼────────────────────────┐            │
│         ▼                        ▼                        ▼            │
│  ┌─────────────┐          ┌─────────────┐          ┌─────────────┐     │
│  │ WebSocket   │          │ Notification│          │  Feedback   │     │
│  │  Services   │          │   Service   │          │   Service   │     │
│  └─────────────┘          └─────────────┘          └─────────────┘     │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                         PostgreSQL                                │  │
│  │  (Orders DB | Agents DB | Partners DB | Feedback DB | Legacy DB) │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Terraform Module Architecture

The Terraform configuration uses a **modular design** that separates application logic from infrastructure:

```
terraform/
├── modules/
│   └── mtogo-app/              # Shared application module
│       ├── main.tf             # Namespace, secrets, configmaps
│       ├── variables.tf        # Input variables
│       ├── services.tf         # All 14 microservices
│       ├── infrastructure.tf   # Optional Kafka/PostgreSQL
│       ├── ingress.tf          # Ingress rules
│       └── outputs.tf          # Module outputs
│
├── local/                      # Local Kubernetes deployment
│   ├── main.tf                 # Uses mtogo-app module
│   ├── variables.tf
│   └── outputs.tf
│
└── azure/                      # Azure AKS deployment
    ├── main.tf                 # Azure resources + mtogo-app module
    ├── variables.tf
    ├── resources.tf            # AKS, PostgreSQL
    └── outputs.tf
```

### Why Modules?

| Benefit             | Description                                            |
| ------------------- | ------------------------------------------------------ |
| **DRY**             | Application logic defined once, used everywhere        |
| **Consistency**     | Same services deployed identically across environments |
| **Flexibility**     | Easy to add AWS, GCP, or other providers               |
| **Maintainability** | Update module, all deployments get the change          |

---

## Local Development with Docker Compose

Docker Compose is the **simplest approach** for local development.

### Quick Start

```powershell
# Start all services
docker-compose up -d

# Start with rebuild
docker-compose up -d --build

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

### Access Points

| Service     | URL                   | Description                                  |
| ----------- | --------------------- | -------------------------------------------- |
| Website     | http://localhost:8081 | Main Blazor frontend                         |
| API Gateway | http://localhost:8080 | API proxy (YARP)                             |
| Legacy API  | http://localhost:8082 | Legacy monolith                              |
| PostgreSQL  | localhost:5432        | Database (user: mtogo, pass: mtogo_password) |
| Kafka       | localhost:9092        | Message broker                               |

---

## Local Kubernetes with Terraform

For testing in a **production-like** environment locally.

### Prerequisites

- Docker Desktop with Kubernetes enabled, OR
- Minikube

### Quick Start

```powershell
# 1. Build Docker images
.\k8s\build-images.ps1

# 2. Deploy with Terraform
cd terraform/local
terraform init
terraform apply

# 3. Access via port-forward
kubectl port-forward -n mtogo svc/website 8081:8080
# Open http://localhost:8081
```

### Using the Helper Script

```powershell
# Build images and deploy
.\terraform\local\deploy-local.ps1 -Build

# Deploy only (images already built)
.\terraform\local\deploy-local.ps1

# Destroy
.\terraform\local\deploy-local.ps1 -Destroy
```

### What Gets Deployed

| Component       | Source              |
| --------------- | ------------------- |
| Namespace       | Module              |
| All 14 Services | Module              |
| PostgreSQL      | Module (in-cluster) |
| Kafka           | Module (in-cluster) |
| Ingress         | Module              |

---

## Azure Production Deployment

The Terraform configuration in `terraform/azure/` deploys a complete production environment to Azure **using the same module as local deployment**.

### What Gets Created

| Resource                       | Description                          |
| ------------------------------ | ------------------------------------ |
| **Resource Group**             | Container for all Azure resources    |
| **AKS Cluster**                | Managed Kubernetes with auto-scaling |
| **PostgreSQL Flexible Server** | Managed database (5 databases)       |
| **NGINX Ingress Controller**   | Load balancer with public IP         |
| **Application Deployments**    | All 14 microservices (from module)   |

### Azure vs Local Comparison

```hcl
# Azure (terraform/azure/main.tf)
module "mtogo_app" {
  source = "../modules/mtogo-app"

  deploy_postgres = false        # Uses Azure PostgreSQL
  deploy_kafka    = true
  database_host   = azurerm_postgresql_flexible_server.main.fqdn
  # ...
}

# Local (terraform/local/main.tf)
module "mtogo_app" {
  source = "../modules/mtogo-app"

  deploy_postgres = true         # Deploys PostgreSQL in cluster
  deploy_kafka    = true
  database_host   = "postgres.mtogo.svc.cluster.local"
  # ...
}
```

### Prerequisites

1. **Azure Account** with active subscription
2. **Azure Service Principal** with Contributor role
3. **GitHub Secrets** configured

### Setting Up Azure Service Principal

```powershell
# Login to Azure
az login

# Create service principal
az ad sp create-for-rbac --name "mtogo-github-actions" `
  --role contributor `
  --scopes /subscriptions/<subscription-id> `
  --sdk-auth
```

### GitHub Secrets Required

| Secret                    | Description                    |
| ------------------------- | ------------------------------ |
| `AZURE_CLIENT_ID`         | Service Principal client ID    |
| `AZURE_CLIENT_SECRET`     | Service Principal secret       |
| `AZURE_SUBSCRIPTION_ID`   | Azure subscription ID          |
| `AZURE_TENANT_ID`         | Azure tenant ID                |
| `POSTGRES_ADMIN_PASSWORD` | Secure password for PostgreSQL |

### Deploying via GitHub Actions

1. Go to **Actions** → **"Deploy to Azure"**
2. Click **"Run workflow"**
3. Configure:
   - **Environment**: `prod` or `staging`
   - **Image tag**: Leave empty for latest
   - **Action**: `plan` (preview) or `apply` (deploy)

### Manual Deployment (CLI)

```powershell
cd terraform/azure

# Configure variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Deploy
terraform init
terraform plan
terraform apply
```

### Auto-Scaling Configuration

```hcl
# In terraform.tfvars
enable_auto_scaling = true
node_count          = 2      # Minimum nodes
node_count_max      = 5      # Maximum nodes
```

### Accessing the Deployed Application

```powershell
# Get AKS credentials
az aks get-credentials --resource-group rg-mtogo-prod --name aks-mtogo-prod

# Check pods
kubectl get pods -n mtogo

# Check services
kubectl get svc -n mtogo

# View logs
kubectl logs -f deployment/order-service -n mtogo
```

### Destroying Azure Infrastructure

⚠️ **WARNING**: Permanently deletes all resources and data!

Via GitHub Actions:

1. Go to **Actions** → **"Destroy Azure Infrastructure"**
2. Select environment
3. Type **DESTROY** to confirm

Via CLI:

```powershell
cd terraform/azure
terraform destroy
```

---

## Switching Cloud Providers

The modular architecture makes it easy to deploy to **any cloud provider** or on-premises Kubernetes.

### Adding AWS Support

Create `terraform/aws/main.tf`:

```hcl
# AWS EKS Cluster
module "eks" {
  source = "terraform-aws-modules/eks/aws"
  # ... EKS configuration
}

# AWS RDS PostgreSQL
resource "aws_db_instance" "postgres" {
  # ... RDS configuration
}

# Use the SAME application module
module "mtogo_app" {
  source = "../modules/mtogo-app"

  namespace           = "mtogo"
  environment         = var.environment
  image_registry      = var.image_registry
  image_tag           = var.image_tag

  deploy_postgres     = false    # Use AWS RDS
  deploy_kafka        = true     # Or use AWS MSK
  database_host       = aws_db_instance.postgres.endpoint
  # ...
}
```

### Adding GCP Support

Create `terraform/gcp/main.tf`:

```hcl
# GCP GKE Cluster
resource "google_container_cluster" "main" {
  # ... GKE configuration
}

# Cloud SQL PostgreSQL
resource "google_sql_database_instance" "postgres" {
  # ... Cloud SQL configuration
}

# Use the SAME application module
module "mtogo_app" {
  source = "../modules/mtogo-app"

  deploy_postgres = false    # Use Cloud SQL
  database_host   = google_sql_database_instance.postgres.ip_address[0].ip_address
  # ...
}
```

### Module Configuration Options

| Variable                     | Description                  | Local      | Azure      | AWS/GCP       |
| ---------------------------- | ---------------------------- | ---------- | ---------- | ------------- |
| `deploy_postgres`            | Deploy PostgreSQL in cluster | ✅ true    | ❌ false   | ❌ false      |
| `deploy_kafka`               | Deploy Kafka in cluster      | ✅ true    | ✅ true    | ✅/❌         |
| `install_ingress_controller` | Install NGINX ingress        | ✅ true    | ✅ true    | ✅ true       |
| `database_host`              | PostgreSQL hostname          | In-cluster | Azure FQDN | RDS/Cloud SQL |

### Benefits of This Approach

1. **Zero code duplication** - Service definitions are shared
2. **Consistent deployments** - Same app config everywhere
3. **Easy testing** - Test locally, deploy to cloud
4. **Provider flexibility** - Switch clouds with minimal effort

---

## CI/CD Pipeline

### Pipeline Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CI/CD Flow                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐       │
│  │  Push/  │───▶│   Detect    │───▶│  Build &    │───▶│   Push to   │       │
│  │   PR    │    │   Changes   │    │   Test      │    │    GHCR     │       │
│  └─────────┘    └─────────────┘    └─────────────┘    └──────┬──────┘       │
│                                                               │              │
│                        ┌──────────────────────────────────────┘              │
│                        ▼                                                     │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Manual Workflows                                  │    │
│  ├─────────────────────┬─────────────────────┬─────────────────────────┤    │
│  │   Deploy to Azure   │   Destroy Azure     │   Staging Test          │    │
│  │   (apply/plan)      │   (confirmation)    │   (integration/load)    │    │
│  └─────────────────────┴─────────────────────┴─────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Available Workflows

| Workflow            | Trigger | Purpose                        |
| ------------------- | ------- | ------------------------------ |
| `cicd.yml`          | Push/PR | Build, test, push images       |
| `deploy-azure.yml`  | Manual  | Deploy to Azure (plan/apply)   |
| `destroy-azure.yml` | Manual  | Tear down Azure infrastructure |
| `staging-test.yml`  | Manual  | Run integration & load tests   |

### Automatic CI (on push/PR)

1. **Change Detection**: Only builds services that changed
2. **Build & Test**: Compiles code and runs unit tests
3. **Docker Build**: Creates container images
4. **Push to GHCR**: Uploads to GitHub Container Registry

### Image Tags

| Branch/Event  | Tag Format       | Example     |
| ------------- | ---------------- | ----------- |
| Main branch   | Commit SHA       | `abc123def` |
| Pull request  | PR SHA           | `xyz789abc` |
| Manual deploy | Specified or SHA | `latest`    |

---

## Prerequisites & Setup

### By Deployment Method

| Component      | Docker Compose | Local Terraform | Azure         |
| -------------- | -------------- | --------------- | ------------- |
| Docker Desktop | ✅ Required    | ✅ Required     | ❌ Optional   |
| Kubernetes     | ❌ Not needed  | ✅ K8s enabled  | ❌ Not needed |
| Terraform      | ❌ Not needed  | ✅ v1.6+        | ✅ v1.6+      |
| Azure CLI      | ❌ Not needed  | ❌ Not needed   | ✅ Required   |
| kubectl        | ❌ Not needed  | ✅ Required     | ✅ Required   |

### Installing Prerequisites

```powershell
# Windows - using Chocolatey
choco install docker-desktop terraform kubernetes-cli azure-cli

# Or using winget
winget install Docker.DockerDesktop
winget install Hashicorp.Terraform
winget install Kubernetes.kubectl
winget install Microsoft.AzureCLI
```

### Enabling Kubernetes in Docker Desktop

1. Open Docker Desktop Settings
2. Go to **Kubernetes** tab
3. Check **Enable Kubernetes**
4. Click **Apply & Restart**

---

## Troubleshooting

### Docker Compose Issues

**Problem**: Services won't start

```powershell
# Check logs for errors
docker-compose logs

# Rebuild from scratch
docker-compose down -v
docker-compose up -d --build
```

**Problem**: Database connection errors

```powershell
# Wait for PostgreSQL to be ready
docker-compose logs postgres

# Ensure health check passes
docker-compose ps
```

### Azure Deployment Issues

**Problem**: Terraform state conflicts

```bash
# Refresh state
cd terraform/azure
terraform refresh -var-file=terraform.tfvars
```

**Problem**: Pods not starting

```bash
# Check pod status
kubectl describe pod <pod-name> -n mtogo

# Check events
kubectl get events -n mtogo --sort-by='.lastTimestamp'
```

**Problem**: Can't pull images from GHCR

```bash
# Verify secret exists
kubectl get secret ghcr-secret -n mtogo

# Check pod image pull errors
kubectl describe pod <pod-name> -n mtogo | grep -A5 "Events"
```

### Common Errors

| Error                        | Solution                                |
| ---------------------------- | --------------------------------------- |
| "No space left on device"    | Run `docker system prune`               |
| "Port already in use"        | Stop conflicting service or change port |
| "GHCR authentication failed" | Check GITHUB_TOKEN permissions          |
| "Terraform state locked"     | Wait or run `terraform force-unlock`    |

---

## Cost Considerations

### Azure Resources

| Resource            | Approximate Monthly Cost |
| ------------------- | ------------------------ |
| AKS (2 × B2s nodes) | ~$60-80                  |
| PostgreSQL (B1ms)   | ~$15-25                  |
| Load Balancer       | ~$20-25                  |
| **Total**           | **~$100-130/month**      |

💡 **Tip**: Use `staging` environment for testing and destroy when not needed.

### Reducing Costs

1. **Use spot instances** for non-production
2. **Scale down** node count during off-hours
3. **Destroy staging** environments when not in use
4. **Use smaller VM sizes** for development

---

## Summary

### Quick Reference

| Task                       | Command/Action                                  |
| -------------------------- | ----------------------------------------------- |
| **Local Dev (Docker)**     | `docker-compose up -d`                          |
| **Local K8s (Terraform)**  | `cd terraform/local && terraform apply`         |
| **Azure (GitHub Actions)** | Actions → Deploy to Azure                       |
| **Azure (CLI)**            | `cd terraform/azure && terraform apply`         |
| **Destroy Azure**          | Actions → Destroy Azure Infrastructure          |
| **View logs**              | `kubectl logs -f deployment/<service> -n mtogo` |

### Architecture Summary

```
terraform/
├── modules/mtogo-app/    # Shared: All 14 services, K8s resources
├── local/                # Uses module + deploys PostgreSQL in-cluster
└── azure/                # Uses module + Azure-managed PostgreSQL

docker-compose.yml        # Standalone local dev (no K8s needed)
```

### Key Design Decisions

1. **Modular Terraform** - Single source of truth for app configuration
2. **Provider Portability** - Same module works with any Kubernetes cluster
3. **Managed Services** - Production uses Azure PostgreSQL; local uses in-cluster
4. **Manual Deployments** - No auto-deploy to production; always reviewed

For questions or issues, please open a GitHub issue in the repository.
