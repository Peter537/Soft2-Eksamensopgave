terraform {
  required_providers {
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }
}

# ===========================================
# Self-signed TLS certificate for ingress
# ===========================================
# NOTE:
# - This enables HTTPS without requiring a DNS name.
# - Browsers will still warn unless the certificate (or its issuing CA) is trusted.
# - For Azure, we bind ingress-nginx to a static public IP so the cert can include
#   the correct IP Subject Alternative Name (SAN).

resource "tls_private_key" "ingress" {
  algorithm = "RSA"
  rsa_bits  = 2048
}

resource "tls_self_signed_cert" "ingress" {
  private_key_pem = tls_private_key.ingress.private_key_pem

  subject {
    common_name  = var.ingress_host
    organization = "MToGo"
  }

  validity_period_hours = 8760 # 1 year

  allowed_uses = [
    "key_encipherment",
    "digital_signature",
    "server_auth",
  ]

  dns_names    = var.ingress_cert_dns_sans
  ip_addresses = var.ingress_cert_ip_sans
}

resource "kubernetes_secret" "ingress_tls" {
  metadata {
    name      = var.ingress_tls_secret_name
    namespace = kubernetes_namespace.mtogo.metadata[0].name
    labels = {
      "app.kubernetes.io/part-of" = "mtogo-platform"
    }
  }

  type = "kubernetes.io/tls"

  data = {
    "tls.crt" = tls_self_signed_cert.ingress.cert_pem
    "tls.key" = tls_private_key.ingress.private_key_pem
  }
}
