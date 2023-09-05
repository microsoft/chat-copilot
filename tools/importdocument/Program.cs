// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace ImportDocument;

/// <summary>
/// This console app imports a list of files to the CopilotChat WebAPI document memory store.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var config = Config.GetConfig();
        if (!Config.Validate(config))
        {
            Console.WriteLine("Error: Failed to read appsettings.json.");
            return;
        }

        var filesOption = new Option<IEnumerable<FileInfo>>(name: "--files", description: "The files to import to document memory store.")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true,
        };

        var chatCollectionOption = new Option<Guid>(
            name: "--chat-id",
            description: "Save the extracted context to an isolated chat collection.",
            getDefaultValue: () => Guid.Empty
        );

        var rootCommand = new RootCommand(
            "This console app imports files to the CopilotChat WebAPI's document memory store."
        )
        {
            filesOption, chatCollectionOption
        };

        rootCommand.SetHandler(async (files, chatCollectionId) =>
            {
                await ImportFilesAsync(files, config!, chatCollectionId);
            },
            filesOption, chatCollectionOption
        );

        rootCommand.Invoke(args);
    }

    /// <summary>
    /// Acquires a user account from Azure AD.
    /// </summary>
    /// <param name="config">The App configuration.</param>
    /// <param name="setAccessToken">Sets the access token to the first account found.</param>
    /// <returns>True if the user account was acquired.</returns>
    private static async Task<bool> AcquireTokenAsync(
        Config config,
        Action<string> setAccessToken)
    {
        Console.WriteLine("Attempting to authenticate user...");

        var webApiScope = $"api://{config.BackendClientId}/{config.Scopes}";
        string[] scopes = { webApiScope };
        try
        {
            var app = PublicClientApplicationBuilder.Create(config.ClientId)
                .WithRedirectUri(config.RedirectUri)
                .WithAuthority(config.Instance, config.TenantId)
                .Build();
            var result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            setAccessToken(result.AccessToken);
            return true;
        }
        catch (Exception ex) when (ex is MsalServiceException or MsalClientException)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Conditionally imports a list of files to the Document Store.
    /// </summary>
    /// <param name="files">A list of files to import.</param>
    /// <param name="config">Configuration.</param>
    /// <param name="chatCollectionId">Save the extracted context to an isolated chat collection.</param>
    private static async Task ImportFilesAsync(IEnumerable<FileInfo> files, Config config, Guid chatCollectionId)
    {
        foreach (var file in files)
        {
            if (!file.Exists)
            {
                Console.WriteLine($"File {file.FullName} does not exist.");
                return;
            }
        }

        string? accessToken = null;
        if (config.AuthenticationType == "AzureAd")
        {
            if (await AcquireTokenAsync(config, v => { accessToken = v; }) == false)
            {
                Console.WriteLine("Error: Failed to acquire access token.");
                return;
            }
            Console.WriteLine($"Successfully acquired access token. Continuing...");
        }

        using var formContent = new MultipartFormDataContent();
        List<StreamContent> filesContent = files.Select(file => new StreamContent(file.OpenRead())).ToList();
        for (int i = 0; i < filesContent.Count; i++)
        {
            formContent.Add(filesContent[i], "formFiles", files.ElementAt(i).Name);
        }

        if (chatCollectionId != Guid.Empty)
        {
            Console.WriteLine($"Uploading and parsing file to chat {chatCollectionId}...");
            using var chatScopeContent = new StringContent("Chat");
            using var chatCollectionIdContent = new StringContent(chatCollectionId.ToString());
            formContent.Add(chatScopeContent, "documentScope");
            formContent.Add(chatCollectionIdContent, "chatId");

            // Calling UploadAsync here to make sure disposable objects are still in scope.
            await UploadAsync(formContent, accessToken, config);
        }
        else
        {
            Console.WriteLine("Uploading and parsing file to global collection...");
            using var globalScopeContent = new StringContent("Global");
            formContent.Add(globalScopeContent, "documentScope");

            // Calling UploadAsync here to make sure disposable objects are still in scope.
            await UploadAsync(formContent, accessToken, config);
        }

        // Dispose of all the file streams.
        foreach (var fileContent in filesContent)
        {
            fileContent.Dispose();
        }
    }

    /// <summary>
    /// Sends a POST request to the Document Store to upload a file for parsing.
    /// </summary>
    /// <param name="multipartFormDataContent">The multipart form data content to send.</param>
    /// <param name="config">Configuration.</param>
    private static async Task UploadAsync(
        MultipartFormDataContent multipartFormDataContent,
        string? accessToken,
        Config config)
    {
        // Create a HttpClient instance and set the timeout to infinite since
        // large documents will take a while to parse.
        using HttpClientHandler clientHandler = new()
        {
            CheckCertificateRevocationList = true
        };
        using HttpClient httpClient = new(clientHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        if (config.AuthenticationType == "AzureAd")
        {
            // Add required properties to the request header.
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken!}");
        }

        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(
                new Uri(new Uri(config.ServiceUri), "importDocuments"),
                multipartFormDataContent
            );

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return;
            }

            Console.WriteLine("Uploading and parsing successful.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
