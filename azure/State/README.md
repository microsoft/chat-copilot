# Introduction 
This is for setting up the initial infrastructure state files storage location. To be run locally once per Azure environment. There is not a backend configured as this is used to set it up for future Terraform scripts.

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

Example naming conventions for Resources: 
https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming
https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations
https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules

Use GEO Codes for Region Codes: https://learn.microsoft.com/en-us/azure/backup/scripts/geo-code-list

# Build and Test
> Run Once in Administrator Mode:  
> ``` 
> Install-Module Az
> Set-ExecutionPolicy RemoteSigned  
> Import-Module Az.Accounts
> ```

1. ` az cloud set --name AzureCloud `
1. ` az login`
1. ` Connect-AzAccount -Environment AzureCloud -Tenant 898fdc18-1bd2-4a3b-84a7-2efb988e3b90 `  
1. ` Select-AzSubscription -Tenant 898fdc18-1bd2-4a3b-84a7-2efb988e3b90 `
1. ` terraform init ` 
1. ` terraform plan ` 
1. ` terraform apply `

Azure Cloud Names:
- AzureCloud
- AzureChinaCloud
- AzureUSGovernment
- AzureGermanCloud

# Helpful Scripts

```powershell 
Connect-AzureRmAccount -Environment AzureCloud -TenantId 898fdc18-1bd2-4a3b-84a7-2efb988e3b90
$locations = Get-AzureRmLocation
$locations | Format-Table -Property Location,DisplayName
```

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)