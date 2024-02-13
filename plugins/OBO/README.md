# API Connector plugin with On-Behhalf-Of Flow for Graph API plugin

> **IMPORTANT:** This sample is for educational purposes only and is not recommended for production deployments.

The goal of the OBO flow is to ensure proper consent is given so that the client app can call the middle-tier app and the middle-tier app has permission to call the back-end resource. 

In this scenario the client app is the chat Web App, the Web Api  is the middle-tier app (as the OBO plugin is native) and the Graph API is the back-end resource.

Read this to understand more about the [OBO Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow#gaining-consent-for-the-middle-tier-application)

> NOTE: This plugin was implemented as a native function, which means that the implementation was done in the same code base of the WebAPI, for that the configuration neeeds to be done in the appsettings.json of the WebAPi code.  

## Requirements

- Enable backend authentication via Azure AD as described in the main readme.md file

## Instructions

1. Add the WebApp (client app) to the "known client application list" in the WebApi app registration, as described [here](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow#gaining-consent-for-the-middle-tier-application)

   - Go to the WebAPp app registration in your tenant and copy the Application Id (Client ID)
   
   - Go to the WebAPI app registration in your tenant

   - Click on "Manifest" option and add an entry for the knownClientApplications attribute using the Application Id (Client ID) of the WebApp registration as described [here](https://learn.microsoft.com/en-us/entra/identity-platform/reference-app-manifest#knownclientapplications-attribute)

   - Save the manifest
 
2. Give the WebApi more delegated permissions

   -  Go to the WebApi API app registration 
   
   - Select the "API permissions" option 
   
   - Click on "+ Add Permission" option abd choose the "Microsoft Graph" option.
   
   - Select "Delegated permission" and choose all the delegated permissions needed 

   - Click on "Add Permissions"
   
   - Make sure you click on "Grant Admin Consent" to the new permissions added

3. Create a Client Secret for the WebAPI app registration OBO Configuration

   - In the WebAPI app registration click on "Certificates & Secrets"

   - Create a new secret by clicking in the "+ New client secret", enter a description and the expiration days

   - Copy the Client Secret and the Application Id (Client ID) to use in the WebAPII appsetting configuration

4. Change the WebAPI appsettings.json file 

   - create a new section as shown below and replace the information with your tenant id, client id and client secret,
  
  ```json
  "OnBehalfOf": {
    "Authority": "https://login.microsoftonline.com",
    "TenantId": "[Replace with your tenant id]",
    "ClientId": "[Replace with the Application Id (Client ID) of the WebAPI registration]",
    "ClientSecret": "[Replace with the Client Secret created for the WebAPI registration]"
  }
   ```   

5. Change the scope for the API Connector plugin in the WebApp code

   - In order to avoid requesting consent from the user for each scope allowed in the WebAPI app registration you need to use the [.default scope]https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow#default-and-combined-consent). The scope name is formed by the Application ID of the WebAPI app registration so it must be updated for a new deployment.

   - To add the scope to the Web App API Connector plugin you need to change the Constants.ts file located in the webapp/src folder, add the ApiConnectorScopes entry as shown below:

   ```typescript
   plugins: {
      // For a list of Microsoft Graph permissions, see https://learn.microsoft.com/en-us/graph/permissions-reference.
      // Your application registration will need to be granted these permissions in Azure Active Directory.
      msGraphScopes: ['Calendars.Read', 'Mail.Read', 'Mail.Send', 'Tasks.ReadWrite', 'User.Read'],
      apiConnectorScopes: ['api://[ENTER THE WE API APPLICATION ID]/.default'],
    }
   ```
   
6. Configuration changes when App is running 

   - run the application as described in main readme.md file 

   - enable the Graph API OBO plugin in the plugins configuration option

   - change the persona to allow the planner to include calls to the plugin

   ```text
   This is a chat between an intelligent AI bot named Copilot and one or more participants. SK stands for Semantic Kernel, the AI platform used to build the bot. 
   The AI bot must execute a Graph API call generating the graph API url with the propper OData query and the Graph scopes to use according to user intent and then summarize the results.  Knowledge cutoff: {{$knowledgeCutoff}} / Current date: {{TimePlugin.Now}}.
   ```

7. Run sample prompts

   - Get the first 3 app registrations in my tenant by calling a graph api and then summarize the results using a table 

   > NOTE: For this prompt to be executed you need to give the WepAPI app registration permission for the scope "Application.Read.All" and the user must be part of a security group that has this permission

   - Get the 3 first groups in my tenant by calling a graph api and then summarize the results using a table     

   > NOTE: For this prompt to be executed you need to give the WepAPI app registration permission for the scope "Group.Read.All" and the user must be part of a security group that has this permission


   ## Known issues

   - When the Copilot uses GPT3.5 turbo the limit of tokens allowed is 8192. When the answer from the Graph API is longer than that limit and error will be thrown. To avoid this try to limit the queries to the top 3-5 elements.
