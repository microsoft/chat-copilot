output "key_vault_secrets_provider_client_id" {
  value = data.azurerm_kubernetes_cluster.aks.key_vault_secrets_provider[0].secret_identity[0].client_id
}

output "key_vault_secrets_provider_name" {
  value = module.azure_keyvault.key_vault_name
}

output "tenant_id" {
  value = data.azurerm_client_config.current.tenant_id
}