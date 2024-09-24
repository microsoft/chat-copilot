##################
# Authentication #
##################

# azure authentication variables
variable "azure_subscription_id" {
  type        = string
  description = "Azure Subscription ID"
}

variable "azure_tenant_id" {
  type        = string
  description = "Azure Tenant ID"
}


##########
# Global #
##########

variable "region_code" {
  type        = string
  description = "Define the region where resources will be created"
}

variable "project_code" {
  type        = string
  description = "Defines the project where resources will be created"

}

variable "environment" {
  type        = string
  description = "Defines the environments to be created and their associated resources e.g. sql server, databases and namespace"
}

variable "tags" {
  type        = map(string)
  description = "Tags to be applied to all resources"
  default = {
    client  = "vision"
    owner   = "david.camden@quartech.com"
    project = "pegasus"
  }
}

##################
# Resource Group #
##################

variable "location" {
  type        = string
  description = "Azure region where the resource group will be created"
}

##################
# Cosmos DB #
##################
variable "cosmosdb_sql_containers" {
  type = list(object({
    name               = string
    partition_key_path = string
  }))
  default = [
    { name = "chatsessions", partition_key_path = "/id" },
    { name = "chatmessages", partition_key_path = "/chatId" }
  ]
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

#######################
# Storage Account #
#######################

variable "container_names" {
  type        = list(string)
  description = "The names of the Containers to be created"
}


###################
# SQL Server #
###################
# variable "database_names" {
#   type        = list(string)
#   description = "The names of the Azure SQL Database to be created for each environment"
# }

# variable "sku_name" {
#   type    = string
#   default = "S0"
# }

###################
# Existing Resources Needed #
###################

variable "kubernetes_cluster_name" {
  type        = string
  description = "Existing AKS Cluster Name where the App will be deployed"
}

variable "kubernetes_resource_group_name" {
  type        = string
  description = "Existing AKS Cluster Resource Group Name where the App will be deployed"
}

variable "kubernetes_azure_subscription_id" {
  type        = string
  description = "Existing AKS Cluster Azure Subscription ID"
}

variable "kubernetes_azure_tenant_id" {
  type        = string
  description = "Existing AKS Cluster Azure Tenant ID"
}
