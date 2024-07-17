output "sql_server_id" {
  description = ""
  value       = azurerm_mssql_server.primary.id
}

output "sql_server_FQDN" {
  description = ""
  value       = azurerm_mssql_server.primary.fully_qualified_domain_name
}

output "sql_server_user_password" {
  sensitive   = true
  value       = random_password.main.result
  description = ""
}

output "sql_server_user_name" {
  value       = azurerm_mssql_server.primary.administrator_login
  description = ""
}