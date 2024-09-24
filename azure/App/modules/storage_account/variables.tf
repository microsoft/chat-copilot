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

variable "tags" {
  description = "A mapping of tags to assign to the resource"
  type        = map(string)
  default     = null
}

variable "name" {
  type        = string
  description = "The name of the Azure Storage account"
}

variable "container_names" {
  type        = list(string)
  description = "The name of the Containers to be created"
}
