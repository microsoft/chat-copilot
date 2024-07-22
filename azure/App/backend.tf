terraform {
  backend "azurerm" {
    container_name       = "sc-copilot-cnc-tfstate"
    resource_group_name  = "rg-copilot-cnc-tfstate"
    storage_account_name = "stcopilottfstate"
    key                  = "app.terraform.tfstate"
    subscription_id      = "b2cba309-26dd-459c-a021-54cb56fe6c49"
    tenant_id            = "898fdc18-1bd2-4a3b-84a7-2efb988e3b90"
  }
}
