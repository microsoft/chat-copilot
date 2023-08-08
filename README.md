# Chat Copilot Sample Application

This sample allows you to build your own integrated large language model (LLM) chat copilot. The sample is built on Microsoft Semantic Kernel and has two components: a frontend [React web app](./webapp/) and a backend [.NET web API service](./webapi/). 

These quick-start instructions run the sample locally. To deploy the sample to Azure, please view [Deploying Chat Copilot](https://github.com/microsoft/chat-copilot/blob/main/deploy/README.md).

> **IMPORTANT:** This sample is for educational purposes only and is not recommended for production deployments.

> **IMPORTANT:** Each chat interaction will call Azure OpenAI/OpenAI which will use tokens that you may be billed for.

![ChatCopilot](https://github.com/microsoft/chat-copilot/assets/64985898/4b5b4ddd-0ba5-4da1-9769-1bc4a74f1996)

# Requirements
You will need the following items to run the sample:

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) _(via Setup script)_
- [Node.js](https://nodejs.org/en/download) _(via Setup script)_
- [Yarn](https://classic.yarnpkg.com/docs/install) _(via Setup script)_
- AI Service

| AI Service   | Requirement                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Azure OpenAI | - [Access](https://aka.ms/oai/access)<br>- [Resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#create-a-resource)<br>- [Deployed models](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#deploy-a-model) (`gpt-35-turbo` and `text-embedding-ada-002`) <br>- [Endpoint](https://learn.microsoft.com/azure/ai-services/openai/tutorials/embeddings?tabs=command-line#retrieve-key-and-endpoint)<br>- [API key](https://learn.microsoft.com/azure/ai-services/openai/tutorials/embeddings?tabs=command-line#retrieve-key-and-endpoint) |
| OpenAI       | - [Account](https://platform.openai.com)<br>- [API key](https://platform.openai.com/account/api-keys)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |

# Instructions

## Windows
1. Open PowerShell as an administrator.
2. Setup your environment.

    ```powershell
    cd <path to chat-copilot>\scripts\
    .\Install.ps1
    ```

    > NOTE: This script will install `Chocolatey`, `dotnet-7.0-sdk`, `nodejs`, and `yarn`.

3. Configure Chat Copilot.
  
    ```powershell
    .\Configure.ps1 -AIService {AI_SERVICE} -APIKey {API_KEY} -Endpoint {AZURE_OPENAI_ENDPOINT}
    ```

    - `AI_SERVICE`: `AzureOpenAI` or `OpenAI`.
    - `API_KEY`: The `API key` for Azure OpenAI or for OpenAI.
    - `AZURE_OPENAI_ENDPOINT`: The Azure OpenAI resource `Endpoint` address. Omit `-Endpoint` if using OpenAI.

    - > **IMPORTANT:** For `AzureOpenAI`, if you deployed models `gpt-35-turbo` and `text-embedding-ada-002` with custom names (instead of each own's given name), also use the parameters:

        ```powershell
        -CompletionModel {DEPLOYMENT_NAME} -EmbeddingModel {DEPLOYMENT_NAME} -PlannerModel {DEPLOYMENT_NAME}
        ```

4. Run Chat Copilot locally. This step starts both the backend API and frontend application.
    
    ```powershell
    .\Start.ps1 
    ```

    It may take a few minutes for Yarn packages to install on the first run.

    > NOTE: Confirm pop-ups are not blocked and you are logged in with the same account used to register the application.

## Linux/macOS
1. Open Bash as an administrator.
2. Configure environment.
  
    ```bash
    cd <path to chat-copilot>/scripts/
    chmod +x *.sh
    ```

   **Ubuntu/Debian Linux**

    ```bash
    ./Install-apt.sh
    ```

    > NOTE: This script uses `apt` to install `dotnet-sdk-7.0`, `nodejs`, and `yarn`.

    **macOS**

    ```bash
    ./Install-brew.sh
    ```

    > NOTE: This script uses `homebrew` to install `dotnet-sdk`, `nodejs`, and `yarn`.

3. Configure Chat Copilot.

    ```bash
    ./Configure.sh --aiservice {AI_SERVICE} --apikey {API_KEY} --endpoint {AZURE_OPENAI_ENDPOINT}
    ```

    - `AI_SERVICE`: `AzureOpenAI` or `OpenAI`.
    - `API_KEY`: The `API key` for Azure OpenAI or for OpenAI.
    - `AZURE_OPENAI_ENDPOINT`: The Azure OpenAI resource `Endpoint` address. Omit `--endpoint` if using OpenAI.

    - > **IMPORTANT:** For `AzureOpenAI`, if you deployed models `gpt-35-turbo` and `text-embedding-ada-002` with custom names (instead of each own's given name), also use the parameters:

        ```bash
        --completionmodel {DEPLOYMENT_NAME} --embeddingmodel {DEPLOYMENT_NAME} --plannermodel {DEPLOYMENT_NAME}
        ```

4. Run Chat Copilot locally. This step starts both the backend API and frontend application.

    ```bash
    ./Start.sh
    ```

    It may take a few minutes for Yarn packages to install on the first run.

    > NOTE: Confirm pop-ups are not blocked and you are logged in with the same account used to register the application.

## (Optional) Enable backend authentication via Azure AD

By default, Chat Copilot runs locally without authentication, using a guest user profile. If you want to enable authentication with Azure Active Directory, follow the steps below.

### Requirements

- [Azure account](https://azure.microsoft.com/free)
- [Azure AD Tenant](https://learn.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant)

### Instructions

1. Create an [application registration](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app) for the frontend web app, using the values below
    - `Supported account types`: "_Accounts in any organizational directory (Any Azure AD directory - Multitenant) and personal Microsoft accounts (e.g. Skype, Xbox)_" 
    - `Redirect URI (optional)`: _Single-page application (SPA)_ and use _http://localhost:3000_.


2. Create a second [application registration](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app) for the backend web api, using the values below:
    - `Supported account types`: "_Accounts in any organizational directory (Any Azure AD directory - Multitenant) and personal Microsoft accounts (e.g. Skype, Xbox)_" 
    - Do **not** configure a `Redirect URI (optional)`

> Take note of the `Application (client) ID` for both app registrations as you will need them in future steps.

3. Expose an API within the second app registration
   1. Select *Expose an API* from the menu

   2. Add an *Application ID URI*
      1. This will generate an `api://` URI with a generated for you

      2. Click *Save* to store the generated URI

   3. Add a scope for `access_as_user`
      1. Click *Add scope*

      2. Set *Scope name* to `access_as_user`

      3. Set *Who can consent* to *Admins and users*

      4. Set *Admin consent display name* and *User consent display name* to `Access copilot chat as a user`

      5. Set *Admin consent description* and *User consent description* to `Allows the accesses to the Copilot chat web API as a user`

4. Add permissions to web app frontend to access web api as user
   1. Open app registration for web app frontend

   2. Go to *API Permissions*

   3. Click *Add a permission*

   4. Select the tab *My APIs*

   5. Choose the app registration representing the web api backend

   6. Select permissions `access_as_user`

   7. Click *Add permissions*

5. Run the Configure script with additional parameters to set up authentication.

    **Powershell**

    ```powershell
    .\Configure.ps1 -AiService {AI_SERVICE} -APIKey {API_KEY} -Endpoint {AZURE_OPENAI_ENDPOINT} -FrontendClientId {FRONTEND_CLIENT_ID} -BackendClientId {BACKEND_CLIENT_ID} -TenantId {TENANT_ID} -Instance {AZURE_AD_INSTANCE}
    ```

    **Bash**
    ```bash
    ./Configure.sh --aiservice {AI_SERVICE} --apikey {API_KEY} --endpoint {AZURE_OPENAI_ENDPOINT} --frontend-clientid {FRONTEND_APPLICATION_ID} --backend-clientid {BACKEND_APPLICATION_ID} --tenantid {TENANT_ID} --instance {AZURE_AD_INSTANCE}
    ```

    - `AI_SERVICE`: `AzureOpenAI` or `OpenAI`.
    - `API_KEY`: The `API key` for Azure OpenAI or for OpenAI.
    - `AZURE_OPENAI_ENDPOINT`: The Azure OpenAI resource `Endpoint` address. Omit `-Endpoint` if using OpenAI.
    - `FRONTEND_APPLICATION_ID`: The `Application (client) ID` associated with the application registration for the frontend.
    - `BACKEND_APPLICATION_ID`: The `Application (client) ID` associated with the application registration for the backend.
    - `TENANT_ID` : Your Azure AD tenant ID
    - `AZURE_AD_INSTANCE` _(optional)_: The Azure AD cloud instance for the authenticating users. Defaults to `https://login.microsoftonline.com/`.

6. Run Chat Copilot locally. This step starts both the backend API and frontend application.

    **Powershell**

    ```powershell
    .\Start.ps1
    ```

    **Bash**

    ```bash
    ./Start.sh
     ```

# Troubleshooting

1. **_Issue:_** Unable to load chats. 
   
    _Details_: interaction_in_progress: Interaction is currently in progress._ 

    _Explanation_: The WebApp can display this error when the application is configured for a different AAD tenant from the browser, (e.g., personal/MSA account vs work/school account). 

    _Solution_: Either use a private/incognito browser tab or clear your browser credentials/cookies. Confirm you are logged in with the same account used to register the application.

2. **_Issue:_**: Challenges using text completion models, such as `text-davinci-003`

    _Solution_: For OpenAI, see [model endpoint compatibility](https://platform.openai.com/docs/models/model-endpoint-compatibility) for
    the complete list of current models supporting chat completions. For Azure OpenAI, see [model summary table and region availability](https://learn.microsoft.com/azure/ai-services/openai/concepts/models#model-summary-table-and-region-availability).

3. **_Issue:_** Localhost SSL certificate errors / CORS errors

    ![Cert-Issue](https://github.com/microsoft/chat-copilot/assets/64985898/e9072af1-e43c-472d-bebc-d0082d0c9180)

    _Explanation_: Your browser may be blocking the frontend access to the backend while waiting for your permission to connect. 

    _Solution_:

    1. Confirm the backend service is running. Open a web browser and navigate to `https://localhost:40443/healthz`
       - You should see a confirmation message: `Healthy`
       - If your browser asks you to acknowledge the risks of visiting an insecure website, you must acknowledge this before the frontend can connect to the backend server. 
    2. Navigate to `http://localhost:3000` or refresh the page to use the Chat Copilot application.

4. **_Issue:_** Yarn is not working.

    _Explanation_: You may have the wrong Yarn version installed such as v2.x+.

    _Solution_: Use the classic version.

    ```bash
    npm install -g yarn
    yarn set version classic
    ```

5. **_Issue:_** Missing `/usr/share/dotnet/host/fxr` folder.

    _Details_: "A fatal error occurred. The folder [/usr/share/dotnet/host/fxr] does not exist" when running dotnet commands on Linux.

    _Explanation_: When .NET (Core) was first released for Linux, it was not yet available in the official Ubuntu repo. So instead, many of us added the Microsoft APT repo in order to install it. Now, the packages are part of the Ubuntu repo, and they are conflicting with the Microsoft packages. This error is a result of mixed packages. ([Source: StackOverflow](https://stackoverflow.com/questions/73753672/a-fatal-error-occurred-the-folder-usr-share-dotnet-host-fxr-does-not-exist))

    _Solution_:

    ```bash
    # Remove all existing packages to get to a clean state:
    sudo apt remove --assume-yes dotnet*;
    sudo apt remove --assume-yes aspnetcore*;
    sudo apt remove --assume-yes netstandard*;

    # Set the Microsoft package provider priority
    echo -e "Package: *\nPin: origin \"packages.microsoft.com\"\nPin-Priority: 1001" | sudo tee /etc/apt/preferences.d/99microsoft-dotnet.pref;

    # Update and install dotnet
    sudo apt update;
    sudo apt install --assume-yes dotnet-sdk-7.0;
    ```
