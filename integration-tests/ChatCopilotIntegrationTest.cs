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
    protected const string BaseAddressSettingName = "BaseAddress";

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

        string? baseAddress = configuration[BaseAddressSettingName];
        Assert.False(string.IsNullOrEmpty(baseAddress));
        Assert.True(baseAddress.EndsWith('/'));

        this._httpClient = new HttpClient();
        this._httpClient.BaseAddress = new Uri(baseAddress);
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
