// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ChatCopilotIntegrationTests;

/// <summary>
/// Base class for Chat Copilot integration tests
/// </summary>
public abstract class ChatCopilotIntegrationTest : IDisposable
{
    protected const string BaseUrlSettingName = "BaseUrl";

    protected readonly HttpClient _httpClient;

    protected ChatCopilotIntegrationTest()
    {
        // Load configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<HealthzTests>()
            .Build();

        string? baseUrl = configuration[BaseUrlSettingName];
        Assert.False(string.IsNullOrEmpty(baseUrl));
        Assert.True(baseUrl.EndsWith('/'));

        this._httpClient = new HttpClient();
        this._httpClient.BaseAddress = new Uri(baseUrl);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._httpClient.Dispose();
        }
    }
}
