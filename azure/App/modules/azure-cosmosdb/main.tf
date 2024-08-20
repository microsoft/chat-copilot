resource "azurerm_cosmosdb_account" "main" {
  name                       = var.name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  offer_type                 = "Standard"
  kind                       = "GlobalDocumentDB"
  automatic_failover_enabled = false
  free_tier_enabled          = false
  geo_location {
    location          = var.location
    failover_priority = 0
  }
  consistency_policy {
    consistency_level       = "BoundedStaleness"
    max_interval_in_seconds = 300
    max_staleness_prefix    = 100000
  }
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "${var.name}-sqldb"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.main.name
  throughput          = var.throughput
}

resource "azurerm_cosmosdb_sql_container" "azurerm_cosmosdb_sql_containers" {
  for_each              = { for record in var.cosmosdb_sql_containers : record.name => record }
  name                  = each.key
  resource_group_name   = var.resource_group_name
  account_name          = azurerm_cosmosdb_account.main.name
  database_name         = azurerm_cosmosdb_sql_database.main.name
  partition_key_paths   = [each.value.partition_key_path]
  partition_key_version = 2
  //throughput            = var.throughput
}
