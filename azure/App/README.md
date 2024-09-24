# Introduction

TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project.

# Getting Started

TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:

1. Installation process
2. Software dependencies
3. Latest releases
4. API references

Example naming conventions for Resources:
https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming
https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations
https://learn.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules

Use GEO Codes for Region Codes: https://learn.microsoft.com/en-us/azure/backup/scripts/geo-code-list

# Build and Test

> Run Once in Administrator Mode:
>
> ```
> Install-Module Az
> Set-ExecutionPolicy RemoteSigned
> Import-Module Az.Accounts
> ```

1. `az cloud set --name AzureCloud `
1. `az login`
1. `az account set --subscription b2cba309-26dd-459c-a021-54cb56fe6c49`
1. `$ACCOUNT_KEY=(az storage account keys list -g rg-copilot-cnc-tfstate -n stcopilottfstate -o tsv --query [0].value)`
1. `$env:ARM_ACCESS_KEY=$ACCOUNT_KEY`
1. `terraform init`
1. `terraform workspace select -or-create test` # default/test/prod
1. `terraform plan -var-file="test.tfvars"` # dev.auto.tfvars/test.tfvars/prod.tfvars
1. `terraform apply -var-file="test.tfvars"` # dev.auto.tfvars/test.tfvars/prod.tfvars

Azure Cloud Names:

- AzureCloud
- AzureChinaCloud
- AzureUSGovernment
- AzureGermanCloud

If you run into this Error:  
`Error retrieving keys for Storage Account "********": storage.AccountsClient#ListKeys: Failure responding to request: StatusCode=404 -- Original Error: autorest/azure: Service returned an error. Status=404 Code="ResourceGroupNotFound" Message="Resource group 'cac-tfstate-vision-pegasus-rsg' could not be found."`

The ARM_ACCESS_KEY is missing a value. Follow the steps to retrieve the Storage Account Key and load the environment variable

If you still encounter problems with selecting the correct subscription, go to https://portal.azure.com and complete login and start the steps above again.

If you run into this Error:
`Error: obtaining Authorization Token from the Azure CLI: parsing json result from the Azure CLI: waiting for the Azure CLI: exit status 1: ERROR: AADSTS50076: Due to a configuration change made by your administrator, or because you moved to a new location, you must use multi-factor authentication to access '797f4846-ba00-4fd7-ba43-dac1f8f63013'`

go to https://portal.azure.com and complete login and start the steps above again.

# Tips and Tricks

```
terraform apply -refresh=false
```

use if you run into troubles with changes that require the cluster to be updated and you run into this error: Kubernetes cluster unreachable: invalid configuration: no configuration has been provided, try setting KUBERNETES_MASTER environment variable

# Contribute

TODO: Explain how other users and developers can contribute to make your code better.

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:

- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)
