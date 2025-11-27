#!/bin/bash
# ===========================================
# MToGo Kubernetes Deployment Script
# ===========================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NAMESPACE="mtogo"

echo "================================================"
echo "  MToGo Kubernetes Deployment"
echo "================================================"

# Check if kubectl is available
if ! command -v kubectl &> /dev/null; then
    echo "Error: kubectl is not installed or not in PATH"
    exit 1
fi

# Check cluster connection
echo "Checking cluster connection..."
if ! kubectl cluster-info &> /dev/null; then
    echo "Error: Cannot connect to Kubernetes cluster"
    exit 1
fi

echo "Connected to cluster: $(kubectl config current-context)"
echo ""

# Step 1: Create namespace
echo "[1/5] Creating namespace..."
kubectl apply -f "$SCRIPT_DIR/namespace.yaml"

# Step 2: Deploy infrastructure (secrets, configmaps)
echo "[2/5] Deploying infrastructure configuration..."
kubectl apply -f "$SCRIPT_DIR/infrastructure/configmap.yaml"
kubectl apply -f "$SCRIPT_DIR/infrastructure/secrets.yaml"

# Step 3: Deploy infrastructure services (PostgreSQL, Kafka)
echo "[3/5] Deploying infrastructure services..."
kubectl apply -f "$SCRIPT_DIR/infrastructure/postgres.yaml"
kubectl apply -f "$SCRIPT_DIR/infrastructure/kafka.yaml"

# Wait for infrastructure to be ready
echo "Waiting for PostgreSQL to be ready..."
kubectl wait --for=condition=ready pod -l app=mtogo-db -n $NAMESPACE --timeout=120s

echo "Waiting for Kafka to be ready..."
kubectl wait --for=condition=ready pod -l app=kafka -n $NAMESPACE --timeout=180s

# Step 4: Deploy application services
echo "[4/5] Deploying application services..."
kubectl apply -f "$SCRIPT_DIR/services/"

# Step 5: Deploy ingress
echo "[5/5] Deploying ingress..."
kubectl apply -f "$SCRIPT_DIR/ingress/"

echo ""
echo "================================================"
echo "  Deployment Complete!"
echo "================================================"
echo ""
echo "Waiting for all pods to be ready..."
kubectl wait --for=condition=ready pod --all -n $NAMESPACE --timeout=300s

echo ""
echo "All pods are running:"
kubectl get pods -n $NAMESPACE

echo ""
echo "Services:"
kubectl get svc -n $NAMESPACE

echo ""
echo "To access the application, add these entries to your hosts file:"
echo "  127.0.0.1 mtogo.local"
echo "  127.0.0.1 api.mtogo.local"
echo "  127.0.0.1 legacy.mtogo.local"
echo ""
echo "Then access:"
echo "  Website: http://mtogo.local"
echo "  API:     http://api.mtogo.local"
