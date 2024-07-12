##################################
# Azure Resource Group variables #
##################################

variable "resource_group_name" {
  type        = string
  description = "The name of an existing Resource Group"
}

variable "location" {
  type        = string
  description = "Define the region where the module should be created, you should use the Resource Group location"
}

######################################
# Azure SQL Server variables #
######################################

variable "sqlserver_name" {
  type        = string
  description = "The name of the Azure SQL Server"
}

variable "database_names" {
  type        = list(string)
  description = "The names of the Azure SQL Database to be created"
}

variable "sku_name" {
  type        = string
  description = "The sku to use when creating Azure the SQL Databases"
}

variable "identity" {
  description = "If you want your SQL Server to have an managed identity. Defaults to false."
  default     = false
}

variable "admin_username" {
  description = "The administrator login name for the new SQL Server"
  default     = null
}

variable "admin_password" {
  description = "The password associated with the admin_username user"
  default     = null
}

variable "sql_database_edition" {
  description = "The edition of the database to be created"
  default     = "Standard"
}

variable "sqldb_service_objective_name" {
  description = " The service objective name for the database"
  default     = "S1"
}


variable "tags" {
  type = map(string)
}

variable "random_password_length" {
  description = "The desired length of random password created by this module"
  default     = 32
}


variable "key_vault_id" {
  type        = string
  description = "The id of the key vault"
}