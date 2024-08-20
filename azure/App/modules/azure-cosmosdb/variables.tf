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
  description = "Define the region the Azure Cosmos DB should be created, you should use the Resource Group location"
}

##################
# Cosmos DB #
##################

variable "name" {
  type        = string
  description = "The name of the Azure Cosmos DB account"
}

variable "cosmosdb_sql_containers" {
  type = list(object({
    name               = string
    partition_key_path = string
  }))
  default = [
    { name = "chatsessions", partition_key_path = "/id" },
    { name = "chatmessages", partition_key_path = "/chatId" }
  ]
  description = "List of Cosmos DB SQL containers to create"
}

variable "throughput" {
  type        = number
  default     = 800
  description = "Cosmos db database throughput"
  validation {
    condition     = var.throughput >= 400 && var.throughput <= 1000000
    error_message = "Cosmos db manual throughput should be equal to or greater than 400 and less than or equal to 1000000."
  }
  validation {
    condition     = var.throughput % 100 == 0
    error_message = "Cosmos db throughput should be in increments of 100."
  }
}
