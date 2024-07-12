resource "azurerm_resource_group" "tfstate" {
  name     = "rg-${local.standard_name}"
  location = var.location
}

resource "azurerm_storage_account" "tfstate" {
  name                     = "st${local.short_name}"
  resource_group_name      = azurerm_resource_group.tfstate.name
  location                 = azurerm_resource_group.tfstate.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  allow_nested_items_to_be_public = false

  tags = merge(var.tags, { environment = "shared" })
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "sc-${local.standard_name}"
  storage_account_name  = azurerm_storage_account.tfstate.name
  container_access_type = "private"
}