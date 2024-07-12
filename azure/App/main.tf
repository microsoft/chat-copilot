data "azurerm_client_config" "current" {}

data "azurerm_kubernetes_cluster" "aks" {
  name                = var.kubernetes_cluster_name
  resource_group_name = var.kubernetes_resource_group_name
}

##################
# resource group #
##################

/*
resource "azurerm_resource_group" "sql" {
  name     = "rg-${local.standard_name}-sql"
  location = var.location
}
*/

resource "azurerm_resource_group" "kv" {
  name     = "rg-${local.standard_name}-kv"
  location = var.location
}



########################
# SQL Database #
########################
/*
module "azure_mssql_database" {
  source              = "./modules/azure-mssql-database"
  sqlserver_name      = "sql-${local.standard_name}"
  database_names      = var.database_names
  sku_name            = var.sku_name
  location            = azurerm_resource_group.sql.location
  resource_group_name = azurerm_resource_group.sql.name
  key_vault_id        = module.azure_keyvault.key_vault_id

  tags = var.tags
  depends_on = [
    module.azure_keyvault
  ]
}
*/

module "kubernetes_namespace" {
  source       = "./modules/kubernetes-namespace"
  environment  = var.environment
  project_code = var.project_code

}


module "azure_keyvault" {
  source              = "./modules/azure-keyvault"
  name                = "kvt-${local.standard_name}"
  location            = azurerm_resource_group.kv.location
  resource_group_name = azurerm_resource_group.kv.name

  enabled_for_deployment          = false
  enabled_for_disk_encryption     = false
  enabled_for_template_deployment = false

  tags = var.tags
}

resource "azurerm_key_vault_access_policy" "vaultaccess" {
  key_vault_id = module.azure_keyvault.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_kubernetes_cluster.aks.key_vault_secrets_provider[0].secret_identity[0].object_id
  # cluster access to secrets should be read-only
  secret_permissions = [
    "Get", "List"
  ]
}

##################
# Azure App Registration
##################







