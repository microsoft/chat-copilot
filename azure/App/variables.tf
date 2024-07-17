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


###################
# SQL Server #
###################
variable "database_names" {
  type        = list(string)
  description = "The names of the Azure SQL Database to be created for each environment"
}

variable "sku_name" {
  type    = string
  default = "S0"
}

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