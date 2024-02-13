# On Bhhalf Of OAuth Flow Graph API plugin

> **IMPORTANT:** This sample is for educational purposes only and is not recommended for production deployments.

The goal of the OBO flow is to ensure proper consent is given so that the client app can call the middle-tier app and the middle-tier app has permission to call the back-end resource. 

In this scenario the client app is the chat Web App, the Web Api  is the middle-tier app (as the OBO plugin is native) and the Graph API is the back-end resource.

Read this to understand more about the [OBO Flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow#gaining-consent-for-the-middle-tier-application)


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

   - In order to avoid commiting secrets to your repo it is recommended you change the appsettings.Development.json as this file will be ignored by GIT

   - create a new section as shown below and replace the information with your tenant id, client id and client secret,
  
  ```json
  "OnBehalfOf": {
    "Authority": "https://login.microsoftonline.com",
    "TenantId": "[Replace with your tenant id]",
    "ClientId": "[Replace with the Application Id (Client ID) of the WebAPI registration]",
    "ClientSecret": "[Replace with the Client Secret created for the WebAPI registration]"
  }
   ```   

5. Configuration changes when App is running 

   - run the application as described in main readme.md file 

   - enable the Graph API OBO plugin in the plugins configuration option

   - change the persona to allow the planner to include calls to the plugin

   ```text
   This is a chat between an intelligent AI bot named Copilot and one or more participants. SK stands for Semantic Kernel, the AI platform used to build the bot. 
   The AI bot must execute a Graph API call generating the graph API url with the propper OData query and the Graph scopes to use according to user intent and then summarize the results.  Knowledge cutoff: {{$knowledgeCutoff}} / Current date: {{TimePlugin.Now}}.
   ```

6. Run sample prompts

   - Get the first 3 app registrations in my tenant by calling a graph api and then summarize the results using a table 

   > NOTE: For this prompt to be executed you need to give the WepAPI app registration permission for the scope "Application.Read.All" and the user must be part of a security group that has this permission

   - Get the 3 first groups in my tenant by calling a graph api and then summarize the results using a table     

   > NOTE: For this prompt to be executed you need to give the WepAPI app registration permission for the scope "Group.Read.All" and the user must be part of a security group that has this permission