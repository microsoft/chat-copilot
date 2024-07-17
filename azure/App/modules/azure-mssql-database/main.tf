resource "random_password" "main" {
  length      = var.random_password_length
  min_upper   = 6
  min_lower   = 4
  min_numeric = 4
  special     = false

  keepers = {
    administrator_login_password = var.sqlserver_name
  }
}

resource "azurerm_key_vault_secret" "mysecret" {
  name         = "DATABASE-USER-PASSWORD"
  value        = var.admin_password == null ? random_password.main.result : var.admin_password
  key_vault_id = var.key_vault_id
}

resource "azurerm_mssql_server" "primary" {
  name                         = var.sqlserver_name
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_username == null ? "sqladmin" : var.admin_username
  administrator_login_password = var.admin_password == null ? random_password.main.result : var.admin_password
  tags                         = var.tags

  dynamic "identity" {
    for_each = var.identity == true ? [1] : [0]
    content {
      type = "SystemAssigned"
    }
  }
}

resource "azurerm_mssql_database" "db" {
  for_each    = toset(var.database_names)
  name        = each.value
  server_id   = azurerm_mssql_server.primary.id
  sku_name    = var.sku_name
  max_size_gb = 5
  tags        = var.tags
}

resource "azurerm_mssql_firewall_rule" "default" {
  name             = "FirewallRuleAzureServices"
  server_id        = azurerm_mssql_server.primary.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
