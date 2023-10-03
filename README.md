# Chat Copilot Sample Application

This sample allows you to build your own integrated large language model (LLM) chat copilot. The sample is built on Microsoft [Semantic Kernel](https://github.com/microsoft/semantic-kernel) and has three components:

1. A frontend application [React web app](./webapp/)
2. A backend REST API [.NET web API service](./webapi/)
3. A [.NET worker service](./memorypipeline/) for processing semantic memory.

These quick-start instructions run the sample locally. They can also be found on the official Chat Copilot Microsoft Learn documentation page for [getting started](https://learn.microsoft.com/semantic-kernel/chat-copilot/getting-started).

To deploy the sample to Azure, please view [Deploying Chat Copilot](./scripts/deploy/README.md) after meeting the [requirements](#requirements) described below.

> **IMPORTANT:** This sample is for educational purposes only and is not recommended for production deployments.

> **IMPORTANT:** Each chat interaction will call Azure OpenAI/OpenAI which will use tokens that you may be billed for.

![Chat Copilot answering a question](https://learn.microsoft.com/en-us/semantic-kernel/media/chat-copilot-in-action.gif)

# Requirements

You will need the following items to run the sample:

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) _(via Setup install.\* script)_
- [Node.js](https://nodejs.org/en/download) _(via Setup install.\* script)_
- [Yarn](https://classic.yarnpkg.com/docs/install) _(via Setup install.\* script)_
- AI Service

| AI Service   | Requirement                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Azure OpenAI | - [Access](https://aka.ms/oai/access)<br>- [Resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#create-a-resource)<br>- [Deployed models](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#deploy-a-model) (`gpt-35-turbo` and `text-embedding-ada-002`) <br>- [Endpoint](https://learn.microsoft.com/azure/ai-services/openai/tutorials/embeddings?tabs=command-line#retrieve-key-and-endpoint)<br>- [API key](https://learn.microsoft.com/azure/ai-services/openai/tutorials/embeddings?tabs=command-line#retrieve-key-and-endpoint) |
| OpenAI       | - [Account](https://platform.openai.com)<br>- [API key](https://platform.openai.com/account/api-keys)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |

# Instructions

## Windows

1. Open PowerShell as an administrator.
   > NOTE: Ensure that you have [PowerShell Core 6+](https://github.com/PowerShell/PowerShell) installed. This is different from the default PowerShell installed on Windows.
2. Setup your environment.

   ```powershell
   cd <path to chat-copilot>\scripts\
   .\Install.ps1
   ```

   > NOTE: This script will install `Chocolatey`, `dotnet-7.0-sdk`, `nodejs`, and `yarn`.

   > NOTE: If you receive an error that the script is not digitally signed or cannot execute on the system, you may need to [change the execution policy](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_execution_policies?view=powershell-7.3#change-the-execution-policy) (see list of [policies](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_execution_policies?view=powershell-7.3#powershell-execution-policies) and [scopes](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_execution_policies?view=powershell-7.3#execution-policy-scope)) or [unblock the script](https://learn.microsoft.com/powershell/module/microsoft.powershell.security/get-executionpolicy?view=powershell-7.3#example-4-unblock-a-script-to-run-it-without-changing-the-execution-policy).

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

   - (Optional) To start ONLY the backend:

     ```powershell
     .\Start-Backend.ps1
     ```

## Linux/macOS

1. Open Bash as an administrator.
2. Configure environment.

   ```bash
   cd <path to chat-copilot>/scripts/
   ```

   **Ubuntu/Debian Linux**

   ```bash
   ./install-apt.sh
   ```

   > NOTE: This script uses `apt` to install `dotnet-sdk-7.0`, `nodejs`, and `yarn`.

   **macOS**

   ```bash
   ./install-brew.sh
   ```

   > NOTE: This script uses `homebrew` to install `dotnet-sdk`, `nodejs`, and `yarn`.

3. Configure Chat Copilot.

   1. For OpenAI

      ```bash
      ./configure.sh --aiservice OpenAI --apikey {API_KEY}
      ```

      - `API_KEY`: The `API key` for OpenAI.

   2. For Azure OpenAI

      ```bash
      ./configure.sh --aiservice AzureOpenAI \
                     --endpoint {AZURE_OPENAI_ENDPOINT} \
                     --apikey   {API_KEY}
      ```

      - `AZURE_OPENAI_ENDPOINT`: The Azure OpenAI resource `Endpoint` address.
      - `API_KEY`: The `API key` for Azure OpenAI.

      **IMPORTANT:** If you deployed models `gpt-35-turbo` and `text-embedding-ada-002`
      with custom names (instead of each own's given name), you need to specify
      the deployment names with three additional parameters:

      ```bash
      ./configure.sh --aiservice AzureOpenAI \
                     --endpoint        {AZURE_OPENAI_ENDPOINT} \
                     --apikey          {API_KEY} \
                     --completionmodel {DEPLOYMENT_NAME} \
                     --plannermodel    {DEPLOYMENT_NAME} \
                     --embeddingmodel  {DEPLOYMENT_NAME}
      ```

4. Run Chat Copilot locally. This step starts both the backend API and frontend application.

   ```bash
   ./start.sh
   ```

   It may take a few minutes for Yarn packages to install on the first run.

   > NOTE: Confirm pop-ups are not blocked and you are logged in with the same account used to register the application.

   - (Optional) To start ONLY the backend:

     ```powershell
     ./start-backend.sh
     ```

## (Optional) Run the [memory pipeline](./memorypipeline/README.md)

By default, the webapi is configured to work without the memory pipeline for synchronous processing documents. To enable asynchronous document processing, you need to configure the webapi and the memory pipeline. Please refer to the [webapi README](./webapi/README.md) and the [memory pipeline README](./memorypipeline/README.md) for more information.

## (Optional) Enable backend authentication via Azure AD

By default, Chat Copilot runs locally without authentication, using a guest user profile. If you want to enable authentication with Azure Active Directory, follow the steps below.

### Requirements

- [Azure account](https://azure.microsoft.com/free)
- [Azure AD Tenant](https://learn.microsoft.com/azure/active-directory/develop/quickstart-create-new-tenant)

### Instructions

1. Create an [application registration](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app) for the frontend web app, using the values below

   - `Supported account types`: "_Accounts in this organizational directory only ({YOUR TENANT} only - Single tenant)_"
   - `Redirect URI (optional)`: _Single-page application (SPA)_ and use _http://localhost:3000_.

2. Create a second [application registration](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app) for the backend web api, using the values below:
   - `Supported account types`: "_Accounts in this organizational directory only ({YOUR TENANT} only - Single tenant)_"
   - Do **not** configure a `Redirect URI (optional)`

> NOTE: Other account types can be used to allow multitenant and personal Microsoft accounts to use your application if you desire. Doing so may result in more users and therefore higher costs.

> Take note of the `Application (client) ID` for both app registrations as you will need them in future steps.

3. Expose an API within the second app registration

   1. Select _Expose an API_ from the menu

   2. Add an _Application ID URI_

      1. This will generate an `api://` URI

      2. Click _Save_ to store the generated URI

   3. Add a scope for `access_as_user`

      1. Click _Add scope_

      2. Set _Scope name_ to `access_as_user`

      3. Set _Who can consent_ to _Admins and users_

      4. Set _Admin consent display name_ and _User consent display name_ to `Access copilot chat as a user`

      5. Set _Admin consent description_ and _User consent description_ to `Allows the accesses to the Copilot chat web API as a user`

   4. Add the web app frontend as an authorized client application

      1. Click _Add a client application_

      2. For _Client ID_, enter the frontend's application (client) ID

      3. Check the checkbox under _Authorized scopes_

      4. Click _Add application_

4. Add permissions to web app frontend to access web api as user

   1. Open app registration for web app frontend

   2. Go to _API Permissions_

   3. Click _Add a permission_

   4. Select the tab _APIs my organization uses_

   5. Choose the app registration representing the web api backend

   6. Select permissions `access_as_user`

   7. Click _Add permissions_

5. Run the Configure script with additional parameters to set up authentication.

   **Powershell**

   ```powershell
   .\Configure.ps1 -AiService {AI_SERVICE} -APIKey {API_KEY} -Endpoint {AZURE_OPENAI_ENDPOINT} -FrontendClientId {FRONTEND_APPLICATION_ID} -BackendClientId {BACKEND_APPLICATION_ID} -TenantId {TENANT_ID} -Instance {AZURE_AD_INSTANCE}
   ```

   **Bash**

   ```bash
   ./configure.sh --aiservice {AI_SERVICE} --apikey {API_KEY} --endpoint {AZURE_OPENAI_ENDPOINT} --frontend-clientid {FRONTEND_APPLICATION_ID} --backend-clientid {BACKEND_APPLICATION_ID} --tenantid {TENANT_ID} --instance {AZURE_AD_INSTANCE}
   ```

   - `AI_SERVICE`: `AzureOpenAI` or `OpenAI`.
   - `API_KEY`: The `API key` for Azure OpenAI or for OpenAI.
   - `AZURE_OPENAI_ENDPOINT`: The Azure OpenAI resource `Endpoint` address. Omit `-Endpoint` if using OpenAI.
   - `FRONTEND_APPLICATION_ID`: The `Application (client) ID` associated with the application registration for the frontend.
   - `BACKEND_APPLICATION_ID`: The `Application (client) ID` associated with the application registration for the backend.
   - `TENANT_ID` : Your Azure AD tenant ID
   - `AZURE_AD_INSTANCE` _(optional)_: The Azure AD cloud instance for the authenticating users. Defaults to `https://login.microsoftonline.com`.

6. Run Chat Copilot locally. This step starts both the backend API and frontend application.

   **Powershell**

   ```powershell
   .\Start.ps1
   ```

   **Bash**

   ```bash
   ./start.sh
   ```

# Troubleshooting

1. **_Issue:_** Unable to load chats.

   _Details_: interaction*in_progress: Interaction is currently in progress.*

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

# Check out our other repos!

If you would like to learn more about Semantic Kernel and AI, you may also be interested in other repos the Semantic Kernel team supports:

| Repo                                                                              | Description                                                                                      |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| [Semantic Kernel](https://github.com/microsoft/semantic-kernel)                   | A lightweight SDK that integrates cutting-edge LLM technology quickly and easily into your apps. |
| [Semantic Kernel Docs](https://github.com/MicrosoftDocs/semantic-kernel-docs)     | The home for Semantic Kernel documentation that appears on the Microsoft learn site.             |
| [Semantic Kernel Starters](https://github.com/microsoft/semantic-kernel-starters) | Starter projects for Semantic Kernel to make it easier to get started.                           |
| [Semantic Memory](https://github.com/microsoft/semantic-memory)                   | A service that allows you to create pipelines for ingesting, storing, and querying knowledge.    |

## Join the community

We welcome your contributions and suggestions to the Chat Copilot Sample App! One of the easiest
ways to participate is to engage in discussions in the GitHub repository.
Bug reports and fixes are welcome!

To learn more and get started:

- Read the [documentation](https://learn.microsoft.com/semantic-kernel/chat-copilot/)
- Join the [Discord community](https://aka.ms/SKDiscord)
- [Contribute](CONTRIBUTING.md) to the project
- Follow the team on our [blog](https://aka.ms/sk/blog)

## Code of Conduct

This project has adopted the
[Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the
[Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](LICENSE) license.
