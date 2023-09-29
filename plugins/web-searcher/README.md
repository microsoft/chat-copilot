# WebSearcher OpenAI Plugin

> **IMPORTANT:** This sample is for educational purposes only and is not recommended for production deployments.

An OpenAI Plugin that can be used to search the internet using Bing.

## Prerequisites:

1. A [Bing Search](https://learn.microsoft.com/en-us/bing/search-apis/bing-web-search/create-bing-search-service-resource) Resource.
2. [Azure Function core tool](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-cli-csharp?tabs=windows%2Cazure-cli#install-the-azure-functions-core-tools).
3. (Optional) An [Azure Function](https://learn.microsoft.com/en-us/azure/azure-functions/functions-get-started?pivots=programming-language-csharp) resource for deployment.

## Local dev

1. Open `local.settings.json` and enter the `BingApiKey`. The ApiKey can be found in your Bing Search resource in Azure portal.
2. Run the following command in a new terminal window

```
> func start
```

## Deploy to Azure

1. Create an Azure function resource.
2. Create and set `PluginConfig:BingApiKey` to the Bing Api key in **Configuration** -> **Application settings** in Azure portal.
3. Publish the package by running

```
dotnet publish --output ./bin/publish --configuration "Release"
```

4. Compress the published binary to a zip by running

```
Compress-Archive -Path .\bin\publish\* -DestinationPath .\bin\publish.zip
```

5. Deploy to Azure via zip push

```
az functionapp deployment source config-zip -g [your resource group name] -n [your function app name] --src .\bin\publish.zip
```

## Usage

You can test the functionality by using the Swagger UI. For example: "{your function url}/swagger/ui"

> Note: The function app does't require any authentication when running locally.
