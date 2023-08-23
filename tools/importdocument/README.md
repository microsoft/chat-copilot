# Copilot Chat Import Document App

> **!IMPORTANT**
> This sample is for educational purposes only and is not recommended for production deployments.

One of the exciting features of the Copilot Chat App is its ability to store contextual information
to [memories](https://github.com/microsoft/semantic-kernel/blob/main/docs/EMBEDDINGS.md) and retrieve
relevant information from memories to provide more meaningful answers to users through out the conversations.

Memories can be generated from conversations as well as imported from external sources, such as documents.
Importing documents enables Copilot Chat to have up-to-date knowledge of specific contexts, such as enterprise and personal data.


## Running the app against a local Chat Copilot instance

1. Ensure the web api is running at `https://localhost:40443/`.
2. Configure the appsettings.json file under this folder root with the following variables:
   - `ServiceUri` is the address the web api is running at
   - `AuthenticationType` should be set to "None"
   - The remaining variables can be left blank or with their default values

3. Change directory to this folder root.
4. **Run** the following command to import a document to the app under the global document collection where
   all users will have access to:

   `dotnet run --files .\sample-docs\ms10k.txt`

   Or **Run** the following command to import a document to the app under a chat isolated document collection where
   only the chat session will have access to:

   `dotnet run --files .\sample-docs\ms10k.txt --chat-id [chatId]`

   > Currently only supports txt and pdf files. A sample file is provided under ./sample-docs.

   Importing may take some time to generate embeddings for each piece/chunk of a document.

   To import multiple files, specify multiple files. For example:

   `dotnet run --files .\sample-docs\ms10k.txt .\sample-docs\Microsoft-Responsible-AI-Standard-v2-General-Requirements.pdf`

5. Chat with the bot.

   Examples:

   With [ms10k.txt](./sample-docs/ms10k.txt):

   ![Document-Memory-Sample-1](https://github.com/microsoft/chat-copilot/assets/52973358/3d35df4d-40f1-4f12-8e40-fd190d5ce127)

   With [Microsoft Responsible AI Standard v2 General Requirements.pdf](./sample-docs/Microsoft-Responsible-AI-Standard-v2-General-Requirements.pdf):

   ![Document-Memory-Sample-2](https://github.com/microsoft/chat-copilot/assets/52973358/f0e95104-72ca-4a0a-9555-ee335d8df696)


## Running the app against a deployed Chat Copilot instance

### Configure your environment

1. Create a registered app in Azure Portal (https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app).

   > Note that this needs to be a separate app registration from those you created when deploying Chat Copilot.

   - Select Mobile and desktop applications as platform type, and the Redirect URI will be `http://localhost`
   - Select **`Accounts in this organizational directory only (Microsoft only - Single tenant)`** as the supported account
     type for this sample.

      > **IMPORTANT:** The supported account type should match that of the backend's app registration. If you changed this setting to allow allow multitenant and personal Microsoft accounts access to your Chat Copilot application, you should change it here as well.

   - Note the **`Application (client) ID`** from your app registration.

2. Update the API permissions in the app registration you just created.

   - From the left-hand menu, select **API permissions** and then **Add a permission**.
   - Select **My APIs** and then select the application corresponding to your backend web api.
   - Check the box next to `access_as_user` and then press **Add permissions**.

3. Update the authorized client applications for your backend web api.
   - In the Azure portal, navigate to your backend web api's app registration.
   - From the left-hand menu, select **Expose an API** and then **Add a client application**.
   - Enter the client ID of the app registration you just created and check the box under **Authorized scopes**. Then press **Add application**.

4. Configure the appsettings.json file under this folder root with the following variables:
   - `ServiceUri` is the address the web api is running at
   - `AuthenticationType` should be set to "AzureAd"
   - `ClientId` is the **Application (client) ID** GUID from the app registration you just created in the Azure Portal
   - `RedirectUri` is the Redirect URI also from the app registration you just created in the Azure Portal (e.g. `http://localhost`)
   - `BackendClientId` is the **Application (client) ID** GUID from your backend web api's app registration in the Azure Portal,
   - `TenantId` is the Azure AD tenant ID that you want to authenticate users against. For single-tenant applications, this is the same as the **Directory (tenant) ID** from your app registration in the Azure Portal.
   - `Instance` is the Azure AD cloud instance to authenticate users against. For most users, this is `https://login.microsoftonline.com`.
   - `Scopes` should be set to "access_as_user"

### Run the app

1. Change directory to this folder root.
2. **Run** the following command to import a document to the app under the global document collection where
   all users will have access to:

   `dotnet run --files .\sample-docs\ms10k.txt`

   Or **Run** the following command to import a document to the app under a chat isolated document collection where
   only the chat session will have access to:

   `dotnet run --files .\sample-docs\ms10k.txt --chat-id [chatId]`

   > Note that both of these commands will open a browser window for you to sign in to ensure you have access to the Chat Copilot service.

   > Currently only supports txt and pdf files. A sample file is provided under ./sample-docs.

   Importing may take some time to generate embeddings for each piece/chunk of a document.

   To import multiple files, specify multiple files. For example:

   `dotnet run --files .\sample-docs\ms10k.txt .\sample-docs\Microsoft-Responsible-AI-Standard-v2-General-Requirements.pdf`
