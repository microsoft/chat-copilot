# API Connector sample plugin using On-Behalf-Of Flow for Graph APIs 

This repository contains a sample API Connector Plugin that uses the On-Behalf-Of (OBO) flow to call Microsoft Graph APIs. 

In this document we will refer to the client app as the WebApp (src/webapp), the middle-tier app as the WebApi (src/webapi) and the backend resource as the GraphApi.

> **IMPORTANT:** This sample is for educational purposes only and is not recommended for production deployments.

> **NOTE:** This plugin was implemented as a native Semantic Kernel function, in the WebAPI code. This is not an implementation of the OpenAI plugin spec. 

## Prerequisites

- Enable backend authentication via Azure AD as described in the main `README.md` file.

## Setup Instructions

1. **Add the WebApp to the "known client application list" in the WebApi app registration.**
   - Go to the WebApp app registration in your tenant and copy the Application Id (Client ID).
   - Go to the WebAPI app registration in your tenant.
   - Click on "Manifest" option and add an entry for the `knownClientApplications` attribute using the Application Id (Client ID) of the WebApp registration as described in this [document](https://learn.microsoft.com/en-us/entra/identity-platform/reference-app-manifest#knownclientapplications-attribute)


   - Save the manifest.

2. **Give the WebApi the delegated permissions.**
   - Go to the WebApi API app registration.
   - Select the "API permissions" option.
   - Click on "+ Add Permission" option and choose the "Microsoft Graph" option.
   - Select "Delegated permission" and choose all the delegated permissions needed.
   - Click on "Add Permissions".
   - As the UI does not implement incremental consent, you need to grant "Admin Consent" to the new permissions added.

3. **Create a Client Secret for the WebAPI app registration OBO Configuration.**
   - In the WebAPI app registration click on "Certificates & Secrets".
   - Create a new secret by clicking in the "+ New client secret", enter a description and the expiration days.
   - Copy the Client Secret and the Application Id (Client ID) to use in the WebAPI appsetting configuration.

4. **Change the WebAPI `appsettings.json` file.**
   - Create a new section as shown below and replace the information with your tenant id, client id and client secret.

```json
"OnBehalfOf": {
  "Authority": "https://login.microsoftonline.com",
  "TenantId": "[Replace with your tenant id]",
  "ClientId": "[Replace with the Application Id (Client ID) of the WebAPI registration]",
  "ClientSecret": "[Replace with the Client Secret created for the WebAPI registration]"
}
```

5. Change the scope for the API Connector plugin in the WebApp code

   - As the UI does noe implement incremental consent, you need to configure the WebApp to use the [.default scope](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow#default-and-combined-consent). The scope name is formed by the Application ID of the WebAPI app registration so you need to update it with the WebApi Application ID (Client ID).

   - Change the Constants.ts file located in the webapp/src folder, add the ApiConnectorScopes entry with the WebApi Application Id, as shown below:

   ```typescript
   plugins: {
      apiConnectorScopes: ['api://[ENTER THE WE API APPLICATION ID]/.default'],
    }
   ```

6. Configuration changes when App is running 

   - Run the application using the instructions from the main readme.md file 

   - Enable the Graph API OBO plugin in the plugins configuration option

   - Change the persona to include calls to the API Connector plugin in the planner

   ```text
   This is a chat between an intelligent AI bot named Copilot and one or more participants. SK stands for Semantic Kernel, the AI platform used to build the bot. 
   The AI bot must execute a Graph API call generating the graph API url with the correct OData query and the Graph scopes to use according to user intent and then summarize the results.  Knowledge cutoff: {{$knowledgeCutoff}} / Current date: {{TimePlugin.Now}}.
   ```

7. Run sample prompts

   - List app registrations prompt: 
      ```text 
      Get the first 3 app registrations in my tenant by calling a graph api and then summarize the results using a table 
      ```
   > NOTE: For this prompt to be executed you need to give the WepAPI app registration permission for the scope "Application.Read.All" and the user must be part of a security group that has this permission

   - List security groups prompt:  
      ```text
      Get the 3 first groups in my tenant by calling a graph api and then summarize the results using a table     
      ```

   > NOTE: For this prompt to be executed you need to give the WepAPI app registration permission for the scope "Group.Read.All" and the user must be part of a security group that has this permission


   ## Known issues

   - Some times Graph API responses are too large and consume all the available tokens required to call the AI model and an error is thrown. Use a reduced query like "Give the first 10 results of ..." to avoid large responses.
