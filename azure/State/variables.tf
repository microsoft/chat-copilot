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

variable "azure_environment" {
  type        = string
  description = "The Cloud Environment which should be used. Possible values are public, usgovernment, and china. Defaults to public."
  default     = "public"
}


##########
# Global #
##########

variable "region_code" {
  type        = string
  description = "Define the region where resources will be created"
}

variable "subscription_code" {
  type        = string
  description = "Define the subscription where resources will be created"
}

variable "project_code" {
  type        = string
  description = "Defines the project where resources will be created"

}

variable "contact_email" {
  type        = string
  description = "Defines the project contact"

}

variable "tags" {
  type = object({
    environment = optional(string)
    client      = string
    owner       = string
    project     = string
  })
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
