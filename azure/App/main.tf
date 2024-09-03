data "azurerm_client_config" "current" {}

data "azurerm_kubernetes_cluster" "aks" {
  name                = var.kubernetes_cluster_name
  resource_group_name = var.kubernetes_resource_group_name

  provider = azurerm.kubernetes
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
  provider = azurerm.kubernetes
}

resource "azurerm_resource_group" "cosmos" {
  name     = "rg-${local.standard_name}-cosmos"
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

  providers = { azurerm = azurerm.kubernetes, kubernetes = kubernetes }

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

  providers = { azurerm = azurerm.kubernetes }
}

resource "azurerm_key_vault_access_policy" "vaultaccess" {
  key_vault_id = module.azure_keyvault.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_kubernetes_cluster.aks.key_vault_secrets_provider[0].secret_identity[0].object_id
  # cluster access to secrets should be read-only
  secret_permissions = [
    "Get", "List"
  ]
  provider = azurerm.kubernetes
}

##################
# Azure App Registration
##################
/*
resource "azuread_application_registration" "example" {
  display_name     = "Example Application"
  description      = "My example application"
  sign_in_audience = "AzureADMyOrg"

  homepage_url          = "https://app.hashitown.com/"
  logout_url            = "https://app.hashitown.com/logout"
  marketing_url         = "https://hashitown.com/"
  privacy_statement_url = "https://hashitown.com/privacy"
  support_url           = "https://support.hashitown.com/"
  terms_of_service_url  = "https://hashitown.com/terms"
}

resource "azuread_application_registration" "frontend" {
  display_name     = "q-pilot-dev"
  description      = "q-pilot Frontend"
  sign_in_audience = "AzureADMyOrg"

  homepage_url = "https://app.hashitown.com/"
  //logout_url            = "https://app.hashitown.com/logout"
  //marketing_url         = "https://hashitown.com/"
  //privacy_statement_url = "https://hashitown.com/privacy"
  //support_url           = "https://support.hashitown.com/"
  //terms_of_service_url  = "https://hashitown.com/terms"
}

resource "azuread_application" "frontend" {
  display_name     = "q-pilot-dev"
  identifier_uris  = ["api://example-app"]
  logo_image       = filebase64("/path/to/logo.png")
  owners           = [data.azuread_client_config.current.object_id]
  sign_in_audience = "AzureADMultipleOrgs"

  api {
    mapped_claims_enabled          = true
    requested_access_token_version = 2

    known_client_applications = [
      azuread_application.known1.application_id,
      azuread_application.known2.application_id,
    ]

    oauth2_permission_scope {
      admin_consent_description  = "Allow the application to access example on behalf of the signed-in user."
      admin_consent_display_name = "Access example"
      enabled                    = true
      id                         = "96183846-204b-4b43-82e1-5d2222eb4b9b"
      type                       = "User"
      user_consent_description   = "Allow the application to access example on your behalf."
      user_consent_display_name  = "Access example"
      value                      = "user_impersonation"
    }

    oauth2_permission_scope {
      admin_consent_description  = "Administer the example application"
      admin_consent_display_name = "Administer"
      enabled                    = true
      id                         = "be98fa3e-ab5b-4b11-83d9-04ba2b7946bc"
      type                       = "Admin"
      value                      = "administer"
    }
  }

  app_role {
    allowed_member_types = ["User", "Application"]
    description          = "Admins can manage roles and perform all task actions"
    display_name         = "Admin"
    enabled              = true
    id                   = "1b19509b-32b1-4e9f-b71d-4992aa991967"
    value                = "admin"
  }

  app_role {
    allowed_member_types = ["User"]
    description          = "ReadOnly roles have limited query access"
    display_name         = "ReadOnly"
    enabled              = true
    id                   = "497406e4-012a-4267-bf18-45a1cb148a01"
    value                = "User"
  }

  feature_tags {
    enterprise = true
    gallery    = true
  }

  optional_claims {
    access_token {
      name = "myclaim"
    }

    access_token {
      name = "otherclaim"
    }

    id_token {
      name                  = "userclaim"
      source                = "user"
      essential             = true
      additional_properties = ["emit_as_roles"]
    }

    saml2_token {
      name = "samlexample"
    }
  }

  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    resource_access {
      id   = "df021288-bdef-4463-88db-98f22de89214" # User.Read.All
      type = "Role"
    }

    resource_access {
      id   = "b4e74841-8e56-480b-be8b-910348b18b4c" # User.ReadWrite
      type = "Scope"
    }
  }

  required_resource_access {
    resource_app_id = "c5393580-f805-4401-95e8-94b7a6ef2fc2" # Office 365 Management

    resource_access {
      id   = "594c1fb6-4f81-4475-ae41-0c394909246c" # ActivityFeed.Read
      type = "Role"
    }
  }

  web {
    homepage_url  = "https://app.example.net"
    logout_url    = "https://app.example.net/logout"
    redirect_uris = ["https://app.example.net/account"]

    implicit_grant {
      access_token_issuance_enabled = true
      id_token_issuance_enabled     = true
    }
  }
}
*/


##################
# Cosmos DB
##################

module "azure_cosmosdb" {
  source                  = "./modules/azure-cosmosdb"
  name                    = "cosmos-${local.standard_name}"
  location                = var.location
  resource_group_name     = azurerm_resource_group.cosmos.name
  cosmosdb_sql_containers = var.cosmosdb_sql_containers
  throughput              = var.throughput
  tags                    = var.tags
}
