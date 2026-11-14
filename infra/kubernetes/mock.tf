variable "mock_image" {
  type = string
}

variable "mock_secrets" {
  type      = map(string)
  sensitive = true
}

resource "kubernetes_persistent_volume_claim_v1" "payment_mock" {
  metadata {
    name      = "payment-mock-data"
    namespace = kubernetes_namespace_v1.service.metadata[0].name
  }
  spec {
    access_modes       = ["ReadWriteOnce"]
    storage_class_name = "standard"
    resources { requests = { storage = "256Mi" } }
  }
  wait_until_bound = false
  lifecycle { prevent_destroy = true }
}

resource "kubernetes_secret_v1" "payment_mock" {
  metadata {
    name      = "payment-mock-secrets"
    namespace = kubernetes_namespace_v1.service.metadata[0].name
  }
  data = var.mock_secrets
}

resource "kubernetes_manifest" "payment_mock_service" {
  manifest   = yamldecode(templatefile("${path.module}/../../k8s/mock-service.yaml.tftpl", local.api_template))
  depends_on = [kubernetes_namespace_v1.service]
}

resource "kubernetes_manifest" "payment_mock" {
  manifest = yamldecode(templatefile("${path.module}/../../k8s/mock-deployment.yaml.tftpl", {
    namespace       = local.namespace
    mock_image      = var.mock_image
    secret_checksum = nonsensitive(sha256(jsonencode(var.mock_secrets)))
    rollout_checksum = sha256(jsonencode([var.mock_image, nonsensitive(sha256(jsonencode(var.mock_secrets))),
    file("${path.module}/../../k8s/mock-deployment.yaml.tftpl")]))
  }))
  depends_on = [kubernetes_persistent_volume_claim_v1.payment_mock, kubernetes_secret_v1.payment_mock,
  kubernetes_manifest.payment_mock_service]
  wait { rollout = true }
  timeouts {
    create = "8m"
    update = "8m"
  }
}
