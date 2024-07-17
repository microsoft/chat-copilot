locals {
  standard_name = "${var.project_code}-${var.region_code}-app-${var.environment}"
  short_name    = "${var.project_code}app${var.environment}"
}