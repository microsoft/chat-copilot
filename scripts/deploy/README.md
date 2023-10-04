# Deploying Chat Copilot

This document details how to deploy Chat Copilot's required resources to your Azure subscription.

## Things to know

- Access to Azure OpenAI is currently limited as we navigate high demand, upcoming product improvements, and Microsoft’s commitment to responsible AI.
  For more details and information on applying for access, go [here](https://learn.microsoft.com/azure/cognitive-services/openai/overview?ocid=AID3051475#how-do-i-get-access-to-azure-openai).
  For regional availability of Azure OpenAI, see the [availability map](https://azure.microsoft.com/explore/global-infrastructure/products-by-region/?products=cognitive-services).
- With the limited availability of Azure OpenAI, consider sharing an Azure OpenAI instance across multiple resources.

- `F1` and `D1` SKUs for the App Service Plans are not currently supported for this deployment in order to support private networking.

- Chat Copilot deployments use Azure Active Directory for authentication. All endpoints (except `/healthz`) require authentication to access.

# Configure your environment

Before you get started, make sure you have the following requirements in place:

- [Azure AD Tenant](https://learn.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant)
- Azure CLI (i.e., az) (if you already installed Azure CLI, make sure to update your installation to the latest version)
  - Windows, go to https://aka.ms/installazurecliwindows
  - Linux, run "`curl -L https://aka.ms/InstallAzureCli | bash`"
- Azure Static Web App CLI (i.e., swa) can be installed by running "`npm install -g @azure/static-web-apps-cli`"
- (Linux only) `zip` can be installed by running "`sudo apt install zip`"

## App registrations (identity)

You will need two Azure Active Directory (AAD) application registrations -- one for the frontend web app and one for the backend API.

> For details on creating an application registration, go [here](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app).

> NOTE: Other account types can be used to allow multitenant and personal Microsoft accounts to use your application if you desire. Doing so may result in more users and therefore higher costs.

### Frontend app registration

- Select `Single-page application (SPA)` as platform type, and set the redirect URI to `http://localhost:3000`
- Select `Accounts in this organizational directory only ({YOUR TENANT} only - Single tenant)` as supported account types.
- Make a note of the `Application (client) ID` from the Azure Portal for use in the `Deploy Frontend` step below.

### Backend app registration

- Do not set a redirect URI
- Select `Accounts in this organizational directory only ({YOUR TENANT} only - Single tenant)` as supported account types.
- Make a note of the `Application (client) ID` from the Azure Portal for use in the `Deploy Azure infrastructure` step below.

### Linking the frontend to the backend

1. Expose an API within the backend app registration

   1. Select _Expose an API_ from the menu

   2. Add an _Application ID URI_

      1. This will generate an `api://` URI

      2. Click _Save_ to store the generated URI

   3. Add a scope for `access_as_user`

      1. Click _Add scope_

      2. Set _Scope name_ to `access_as_user`

      3. Set _Who can consent_ to _Admins and users_

      4. Set _Admin consent display name_ and _User consent display name_ to `Access Chat Copilot as a user`

      5. Set _Admin consent description_ and _User consent description_ to `Allows the accesses to the Chat Copilot web API as a user`

   4. Add the web app frontend as an authorized client application

      1. Click _Add a client application_

      2. For _Client ID_, enter the frontend's application (client) ID

      3. Check the checkbox under _Authorized scopes_

      4. Click _Add application_

2. Add permissions to web app frontend to access web api as user

   1. Open app registration for web app frontend

   2. Go to _API Permissions_

   3. Click _Add a permission_

   4. Select the tab _APIs my organization uses_

   5. Choose the app registration representing the web api backend

   6. Select permissions `access_as_user`

   7. Click _Add permissions_

# Deploy Azure Infrastructure

The examples below assume you are using an existing Azure OpenAI resource. See the notes following each command for using OpenAI or creating a new Azure OpenAI resource.

## PowerShell

```powershell
./deploy-azure.ps1 -Subscription {YOUR_SUBSCRIPTION_ID} -DeploymentName {YOUR_DEPLOYMENT_NAME} -AIService {AzureOpenAI or OpenAI} -AIApiKey {YOUR_AI_KEY} -AIEndpoint {YOUR_AZURE_OPENAI_ENDPOINT} -BackendClientId {YOUR_BACKEND_APPLICATION_ID} -TenantId {YOUR_TENANT_ID}
```

- To use an existing Azure OpenAI resource, set `-AIService` to `AzureOpenAI` and include `-AIApiKey` and `-AIEndpoint`.
- To deploy a new Azure OpenAI resource, set `-AIService` to `AzureOpenAI` and omit `-AIApiKey` and `-AIEndpoint`.
- To use an an OpenAI account, set `-AIService` to `OpenAI` and include `-AIApiKey`.

## Bash

```bash
chmod +x ./deploy-azure.sh
./deploy-azure.sh --subscription {YOUR_SUBSCRIPTION_ID} --deployment-name {YOUR_DEPLOYMENT_NAME} --ai-service {AzureOpenAI or OpenAI} --ai-service-key {YOUR_AI_KEY} --ai-endpoint {YOUR_AZURE_OPENAI_ENDPOINT} --client-id {YOUR_BACKEND_APPLICATION_ID} --tenant-id {YOUR_TENANT_ID}
```

- To use an existing Azure OpenAI resource, set `--ai-service` to `AzureOpenAI` and include `--ai-service-key` and `--ai-endpoint`.
- To deploy a new Azure OpenAI resource, set `--ai-service` to `AzureOpenAI` and omit `--ai-service-key` and `--ai-endpoint`.
- To use an an OpenAI account, set `--ai-service` to `OpenAI` and include `--ai-service-key`.

## Azure Portal

You can also deploy the infrastructure directly from the Azure Portal by clicking the button below:

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://aka.ms/sk-deploy-existing-azureopenai-portal)

> This will automatically deploy the most recent release of CopilotChat backend binaries ([link](https://github.com/microsoft/chat-copilot/releases)).

> To find the deployment name when using `Deploy to Azure`, look for a deployment in your resource group that starts with `Microsoft.Template`.

# Deploy Backend (WebAPI)

> **_NOTE:_** This step can be skipped if the previous Azure Resources creation step succeeded without errors. The `deployWebApiPackage = true` setting in main.bicep ensures that the latest copilot chat api is deployed.

To deploy the backend, build the deployment package first and deploy it to the Azure resources created above.

## PowerShell

```powershell
./package-webapi.ps1

./deploy-webapi.ps1 -Subscription {YOUR_SUBSCRIPTION_ID} -ResourceGroupName rg-{YOUR_DEPLOYMENT_NAME} -DeploymentName {YOUR_DEPLOYMENT_NAME}
```

## Bash

```bash
chmod +x ./package-webapi.sh
./package-webapi.sh

chmod +x ./deploy-webapi.sh
./deploy-webapi.sh --subscription {YOUR_SUBSCRIPTION_ID} --resource-group rg-{YOUR_DEPLOYMENT_NAME} --deployment-name {YOUR_DEPLOYMENT_NAME}
```

# Deploy Hosted Plugins

> **_NOTE:_** This step can be skipped if the previous Azure Resources creation step succeeded without errors. The `deployWebSearcherPackage = true` setting in main.bicep ensures that the WebSearcher is deployed.
> **_NOTE:_** More hosted plugins will be available.

To deploy the plugins, build the packages first and deploy then to the Azure resources created above.

## PowerShell

```powershell
./package-plugins.ps1

./deploy-plugins.ps1 -Subscription {YOUR_SUBSCRIPTION_ID} -ResourceGroupName rg-{YOUR_DEPLOYMENT_NAME} -DeploymentName {YOUR_DEPLOYMENT_NAME}
```

## Bash

```bash
chmod +x ./package-plugins.sh
./package-webapi.sh

chmod +x ./deploy-plugins.sh
./deploy-webapi.sh --subscription {YOUR_SUBSCRIPTION_ID} --resource-group rg-{YOUR_DEPLOYMENT_NAME} --deployment-name {YOUR_DEPLOYMENT_NAME}
```

# Deploy Frontend (WebApp)

## Prerequisites

### Install Azure's Static Web Apps CLI

```bash
npm install -g @azure/static-web-apps-cli
```

## PowerShell

```powershell

./deploy-webapp.ps1 -Subscription {YOUR_SUBSCRIPTION_ID} -ResourceGroupName rg-{YOUR_DEPLOYMENT_NAME} -DeploymentName {YOUR_DEPLOYMENT_NAME} -FrontendClientId {YOUR_FRONTEND_APPLICATION_ID} -PackageFilePath .\out\webapi.zip
```

## Bash

```bash
./deploy-webapp.sh --subscription {YOUR_SUBSCRIPTION_ID} --resource-group rg-{YOUR_DEPLOYMENT_NAME} --deployment-name {YOUR_DEPLOYMENT_NAME} --client-id {YOUR_FRONTEND_APPLICATION_ID}
```

# (Optional) Deploy Memory Pipeline

> **_NOTE:_** This step can be skipped if the WebApi is not configured to run asynchronously for document processing. By default, the WebApi is configured to run asynchronously for document processing in deployment.

> **_NOTE:_** This step can be skipped if the previous Azure Resources creation step succeeded without errors. The deployMemoryPipelinePackage = true setting in main.bicep ensures that the latest copilot chat memory pipeline is deployed.

To deploy the memorypipeline, build the deployment package first and deploy it to the Azure resources created above.

## PowerShell

```powershell
./package-memorypipeline.ps1

./deploy-memorypipeline.ps1 -Subscription {YOUR_SUBSCRIPTION_ID} -ResourceGroupName rg-{YOUR_DEPLOYMENT_NAME} -DeploymentName {YOUR_DEPLOYMENT_NAME}
```

## Bash

```bash
chmod +x ./package-memorypipeline.sh
./package-memorypipeline.sh

chmod +x ./deploy-memorypipeline.sh
./deploy-memorypipeline.sh --subscription {YOUR_SUBSCRIPTION_ID} --resource-group rg-{YOUR_DEPLOYMENT_NAME} --deployment-name {YOUR_DEPLOYMENT_NAME}
```

Your Chat Copilot application is now deployed!

# Appendix

## Using custom web frontends to access your deployment

Make sure to include your frontend's URL as an allowed origin in your deployment's CORS settings. Otherwise, web browsers will refuse to let JavaScript make calls to your deployment.

To do this, go on the Azure portal, select your Semantic Kernel App Service, then click on "CORS" under the "API" section of the resource menu on the left of the page.
This will get you to the CORS page where you can add your allowed hosts.

### PowerShell

```powershell
$webApiName = $(az deployment group show --name {DEPLOYMENT_NAME} --resource-group rg-{DEPLOYMENT_NAME} --output json | ConvertFrom-Json).properties.outputs.webapiName.value

($(az webapp config appsettings list --name $webapiName --resource-group rg-{YOUR_DEPLOYMENT_NAME} | ConvertFrom-JSON) | Where-Object -Property name -EQ -Value Authorization:ApiKey).value
```

### Bash

```bash
eval WEB_API_NAME=$(az deployment group show --name $DEPLOYMENT_NAME --resource-group $RESOURCE_GROUP --output json) | jq -r '.properties.outputs.webapiName.value'

$(az webapp config appsettings list --name $WEB_API_NAME --resource-group rg-{YOUR_DEPLOYMENT_NAME} | jq '.[] | select(.name=="Authorization:ApiKey").value')
```
