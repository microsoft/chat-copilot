# Chat Copilot Integration Tests

## Requirements

1. A running instance of the Chat Copilot's [backend](../webapi/README.md).

## Setup

### Option 1: Use Secret Manager

Integration tests require the URL of the backend instance.

We suggest using the .NET [Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
to avoid the risk of leaking secrets into the repository, branches and pull requests.

Values set using the Secret Manager will override the settings set in the `testsettings.development.json` file and in environment variables.

To set your secrets with Secret Manager:

```ps
cd integration-tests

dotnet user-secrets init
dotnet user-secrets set "BaseUrl" "https://your-backend-address/"
```

### Option 2: Use a Configuration File

1. Create a `testsettings.development.json` file next to `testsettings.json`. This file will be ignored by git,
   the content will not end up in pull requests, so it's safe for personal settings. Keep the file safe.
2. Edit `testsettings.development.json` and
    1. Set your base address - **make sure it ends with a trailing '/' **

For example:

```json
{
  "BaseUrl": "https://localhost:40443/"
}
```

### Option 3: Use Environment Variables
You may also set the test settings in your environment variables. The environment variables will override the settings in the `testsettings.development.json` file.

- bash:

```bash
export BaseUrl="https://localhost:40443/"
```

- PowerShell:

```ps
$env:BaseUrl = "https://localhost:40443/"
```
