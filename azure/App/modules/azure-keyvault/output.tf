output "key_vault_name" {
  description = "Key Vault Name"
  value       = azurerm_key_vault.kv.name
}

output "key_vault_id" {
  description = "Key Vault ID"
  value       = azurerm_key_vault.kv.id
}

output "key_vault_url" {
  description = "Key Vault URI"
  value       = azurerm_key_vault.kv.vault_uri
}

