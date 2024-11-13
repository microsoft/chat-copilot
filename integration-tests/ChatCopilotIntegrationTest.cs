// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Xunit;

namespace ChatCopilotIntegrationTests;

/// <summary>
/// Base class for Chat Copilot integration tests
/// </summary>
[Trait("Category", "Integration Tests")]
#pragma warning disable CA1051
public abstract class ChatCopilotIntegrationTest : IDisposable
{
    protected const string BaseUrlSettingName = "BaseServerUrl";
    protected const string ClientIdSettingName = "ClientID";
    protected const string AuthoritySettingName = "Authority";
    protected const string UsernameSettingName = "TestUsername";
    protected const string PasswordSettingName = "TestPassword";
    protected const string ScopesSettingName = "Scopes";

    protected readonly HttpClient HTTPClient;
    protected readonly IConfigurationRoot Configuration;

    protected ChatCopilotIntegrationTest()
    {
        // Load configuration
        this.Configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<HealthzTests>()
            .Build();

        string? baseUrl = this.Configuration[BaseUrlSettingName];
        Assert.False(string.IsNullOrEmpty(baseUrl));
        Assert.True(baseUrl.EndsWith('/'));

        this.HTTPClient = new HttpClient();
        this.HTTPClient.BaseAddress = new Uri(baseUrl);
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
            this.HTTPClient.Dispose();
        }
    }

    protected async Task SetUpAuthAsync()
    {
        string accesstoken = await this.GetUserTokenByPasswordAsync();
        Assert.True(!string.IsNullOrEmpty(accesstoken));

        this.HTTPClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accesstoken);
    }

    protected async Task<string> GetUserTokenByPasswordAsync()
    {
        IPublicClientApplication app = PublicClientApplicationBuilder.Create(this.Configuration[ClientIdSettingName])
            .WithAuthority(this.Configuration[AuthoritySettingName])
            .Build();

        string? scopeString = this.Configuration[ScopesSettingName];
        Assert.NotNull(scopeString);

        string[] scopes = scopeString.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);

        var accounts = await app.GetAccountsAsync();

        AuthenticationResult? result = null;

        if (accounts.Any())
        {
            result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
        }
        else
        {
            result = await app.AcquireTokenByUsernamePassword(scopes, this.Configuration[UsernameSettingName], this.Configuration[PasswordSettingName]).ExecuteAsync();
        }

        return result?.AccessToken ?? string.Empty;
    }
}
