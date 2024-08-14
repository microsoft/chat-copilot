resource "random_pet" "prefix" {
  prefix = var.prefix
  length = 1
}

resource "azurerm_resource_group" "example" {
  name     = "${random_pet.prefix.id}-rg"
  location = var.location
}

resource "azurerm_cosmosdb_account" "example" {
  name                      = random_pet.prefix.id
  location                  = var.cosmosdb_account_location
  resource_group_name       = azurerm_resource_group.example.name
  offer_type                = "Standard"
  kind                      = "GlobalDocumentDB"
  enable_automatic_failover = false
  enable_free_tier          = true
  geo_location {
    location          = var.location
    failover_priority = 0
  }
  consistency_policy {
    consistency_level       = "BoundedStaleness"
    max_interval_in_seconds = 300
    max_staleness_prefix    = 100000
  }
  depends_on = [
    azurerm_resource_group.example
  ]
}

resource "azurerm_cosmosdb_sql_database" "main" {
  name                = "${random_pet.prefix.id}-cosmosdb-sqldb"
  resource_group_name = azurerm_resource_group.example.name
  account_name        = azurerm_cosmosdb_account.example.name
  throughput          = var.throughput
}

resource "azurerm_cosmosdb_sql_container" "chat_sessions" {
  name                  = "chatsessions"
  resource_group_name   = azurerm_resource_group.example.name
  account_name          = azurerm_cosmosdb_account.example.name
  database_name         = azurerm_cosmosdb_sql_database.main.name
  partition_key_path    = "/id"
  partition_key_version = 2
  throughput            = var.throughput
}

resource "azurerm_cosmosdb_sql_container" "chat_messages" {
  name                  = "chatmessages"
  resource_group_name   = azurerm_resource_group.example.name
  account_name          = azurerm_cosmosdb_account.example.name
  database_name         = azurerm_cosmosdb_sql_database.main.name
  partition_key_path    = "/chatId"
  partition_key_version = 2
  throughput            = var.throughput
}

resource "azurerm_cosmosdb_sql_container" "chat_memory_sources" {
  name                  = "chatmemorysources"
  resource_group_name   = azurerm_resource_group.example.name
  account_name          = azurerm_cosmosdb_account.example.name
  database_name         = azurerm_cosmosdb_sql_database.main.name
  partition_key_path    = "/chatId"
  partition_key_version = 2
  throughput            = var.throughput
}

resource "azurerm_cosmosdb_sql_container" "chat_participants" {
  name                  = "chatparticipants"
  resource_group_name   = azurerm_resource_group.example.name
  account_name          = azurerm_cosmosdb_account.example.name
  database_name         = azurerm_cosmosdb_sql_database.main.name
  partition_key_path    = "/chatId"
  partition_key_version = 2
  throughput            = var.throughput
}