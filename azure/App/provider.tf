# define terraform provider
terraform {
  required_version = ">= 1.8.5"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.116"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~>2.32"
    }
  }
}

provider "kubernetes" {
  host                   = data.azurerm_kubernetes_cluster.aks.kube_config.0.host
  client_certificate     = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config.0.client_certificate)
  client_key             = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config.0.client_key)
  cluster_ca_certificate = base64decode(data.azurerm_kubernetes_cluster.aks.kube_config.0.cluster_ca_certificate)
}

# configure the azure provider
provider "azurerm" {
  environment     = "public"
  subscription_id = var.azure_subscription_id
  tenant_id       = var.azure_tenant_id
  features {}
}

provider "azurerm" {
  environment     = "public"
  subscription_id = var.kubernetes_azure_subscription_id
  tenant_id       = var.kubernetes_azure_tenant_id
  features {}

  alias = "kubernetes"
}
