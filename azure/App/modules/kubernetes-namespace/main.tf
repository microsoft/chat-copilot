resource "kubernetes_namespace" "default" {
  metadata {
    name = "${var.project_code}-${var.environment}"

    annotations = {
      "app" : var.project_code
    }
    labels = {
      "env" : var.environment,
      "owner" : var.project_code,
      "managed-by" : "Quartech"
    }
  }
}

