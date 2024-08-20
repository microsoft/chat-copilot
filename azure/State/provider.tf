terraform {
  required_version = ">=1.8.5"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.116"
    }
  }
}

provider "azurerm" {
  environment     = "public"
  subscription_id = var.azure_subscription_id
  tenant_id       = var.azure_tenant_id
  features {}
}
