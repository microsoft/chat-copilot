####################
# Global Variables #
####################

variable "tags" {
  description = "A mapping of tags to assign to the resource"
  type        = map(string)
  default     = null
}

##################################
# Azure Resource Group variables #
##################################

variable "resource_group_name" {
  type        = string
  description = "The name of an existing Resource Group"
}

variable "location" {
  type        = string
  description = "Define the region the Azure Key Vault should be created, you should use the Resource Group location"
}

############################# 
# Azure Key Vault variables #
#############################

variable "name" {
  type        = string
  description = "The name of the Azure Key Vault"
}

variable "sku_name" {
  type        = string
  description = "Select Standard or Premium SKU"
  default     = "standard"
}

variable "enabled_for_deployment" {
  type        = string
  description = "Allow Azure Virtual Machines to retrieve certificates stored as secrets from the Azure Key Vault"
  default     = "true"
}

variable "enabled_for_disk_encryption" {
  type        = string
  description = "Allow Azure Disk Encryption to retrieve secrets from the Azure Key Vault and unwrap keys"
  default     = "true"
}

variable "enabled_for_template_deployment" {
  type        = string
  description = "Allow Azure Resource Manager to retrieve secrets from the Azure Key Vault"
  default     = "true"
}

variable "kv_key_permissions_full" {
  type        = list(string)
  description = "List of full key permissions, must be one or more from the following: Get, List, Update, Create, Import, Delete, Recover, Backup, Restore, Decrypt, Encrypt, UnwrapKey, WrapKey, Verify, Sign, Purge, Release, Rotate, GetRotationPolicy, SetRotationPolicy"
  default     = ["Get", "List", "Update", "Create", "Import", "Delete", "Recover", "Backup", "Restore", "Decrypt", "Encrypt", "UnwrapKey", "WrapKey", "Verify", "Sign", "Purge", "Release", "Rotate", "GetRotationPolicy", "SetRotationPolicy"]
}

variable "kv_secret_permissions_full" {
  type        = list(string)
  description = "List of full secret permissions, must be one or more from the following: Get, List, Set, Delete, Recover, Backup, Restore, Purge"
  default     = ["Get", "List", "Set", "Delete", "Recover", "Backup", "Restore", "Purge"]
}

variable "kv_certificate_permissions_full" {
  type        = list(string)
  description = "List of full certificate permissions, must be one or more from the following: Get, List, Update, Create, Import, Delete, Recover, Backup, Restore, ManageContacts, ManageIssuers, GetIssuers, ListIssuers, SetIssuers, DeleteIssuers, Purge"
  default     = ["Get", "List", "Update", "Create", "Import", "Delete", "Recover", "Backup", "Restore", "ManageContacts", "ManageIssuers", "GetIssuers", "ListIssuers", "SetIssuers", "DeleteIssuers", "Purge"]
}

variable "kv_storage_permissions_full" {
  type        = list(string)
  description = "List of full storage permissions, must be one or more from the following: Backup, Delete, DeleteSAS, Get, GetSAS, List, ListSAS, Purge, Recover, RegenerateKey, Restore, Set, SetSAS, Update"
  default     = ["Backup", "Delete", "DeleteSAS", "Get", "GetSAS", "List", "ListSAS", "Purge", "Recover", "RegenerateKey", "Restore", "Set", "SetSAS", "Update"]
}

variable "kv_key_permissions_read" {
  type        = list(string)
  description = "List of read key permissions, must be one or more from the following: Get, List"
  default     = ["Get", "List"]
}

variable "kv_secret_permissions_read" {
  type        = list(string)
  description = "List of full secret permissions, must be one or more from the following: Get, List"
  default     = ["Get", "List"]
}

variable "kv_certificate_permissions_read" {
  type        = list(string)
  description = "List of full certificate permissions, must be one or more from the following: Get, GetIssuers, List, ListIssuers"
  default     = ["Get", "GetIssuers", "List", "ListIssuers"]
}

variable "kv_storage_permissions_read" {
  type        = list(string)
  description = "List of read storage permissions, must be one or more from the following: Get, GetSAS, List, ListSAS"
  default     = ["Get", "GetSAS", "List", "ListSAS"]
}