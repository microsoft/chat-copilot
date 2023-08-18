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
   1. Select *Expose an API* from the menu

   2. Add an *Application ID URI*
      1. This will generate an `api://` URI

      2. Click *Save* to store the generated URI

   3. Add a scope for `access_as_user`
      1. Click *Add scope*

      2. Set *Scope name* to `access_as_user`

      3. Set *Who can consent* to *Admins and users*

      4. Set *Admin consent display name* and *User consent display name* to `Access Chat Copilot as a user`

      5. Set *Admin consent description* and *User consent description* to `Allows the accesses to the Chat Copilot web API as a user`

4. Add permissions to web app frontend to access web api as user
   1. Open app registration for web app frontend

   2. Go to *API Permissions*

   3. Click *Add a permission*

   4. Select the tab *My APIs*

   5. Choose the app registration representing the web api backend

   6. Select permissions `access_as_user`

   7. Click *Add permissions*


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

> This will automatically deploy the most recent release of CopilotChat backend binaries ([link](https://github.com/microsoft/copilot-chat/releases)).

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

# Deploy Frontend (WebApp)

## Prerequisites

### Install Azure's Static Web Apps CLI

```bash
npm install -g @azure/static-web-apps-cli
```

## PowerShell

```powershell

./deploy-webapp.ps1 -Subscription {YOUR_SUBSCRIPTION_ID} -ResourceGroupName rg-{YOUR_DEPLOYMENT_NAME} -DeploymentName {YOUR_DEPLOYMENT_NAME} -FrontendClientId {YOUR_FRONTEND_APPLICATION_ID}
```

## Bash

```bash
./deploy-webapp.sh --subscription {YOUR_SUBSCRIPTION_ID} --resource-group rg-{YOUR_DEPLOYMENT_NAME} --deployment-name {YOUR_DEPLOYMENT_NAME} --client-id {YOUR_FRONTEND_APPLICATION_ID}
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
