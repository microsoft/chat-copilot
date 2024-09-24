resource "azurerm_storage_account" "stg" {
  name                     = var.name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"
  min_tls_version          = "TLS1_2"
  tags                     = var.tags

  identity {
    type = "SystemAssigned"
  }

}

resource "azurerm_storage_container" "stg" {
  for_each              = toset(var.container_names)
  name                  = each.value
  storage_account_name  = azurerm_storage_account.stg.name
  container_access_type = "blob"
  metadata              = null
}

