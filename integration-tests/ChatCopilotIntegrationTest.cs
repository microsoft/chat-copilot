// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Xunit;

namespace ChatCopilotIntegrationTests;

/// <summary>
/// Base class for Chat Copilot integration tests
/// </summary>
[Trait("Category", "Integration Tests")]
public abstract class ChatCopilotIntegrationTest : IDisposable
{
    protected const string BaseUrlSettingName = "BaseServerUrl";
    protected const string ClientIdSettingName = "ClientID";
    protected const string AuthoritySettingName = "Authority";
    protected const string UsernameSettingName = "TestUsername";
    protected const string PasswordSettingName = "TestPassword";
    protected const string ScopesSettingName = "Scopes";

    protected readonly HttpClient _httpClient;
    protected readonly IConfigurationRoot configuration;

    protected ChatCopilotIntegrationTest()
    {
        // Load configuration
        this.configuration = new ConfigurationBuilder()
            .AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<HealthzTests>()
            .Build();

        string? baseUrl = this.configuration[BaseUrlSettingName];
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

    protected async Task SetUpAuth()
    {
        string accesstoken = await this.GetUserTokenByPassword();
        Assert.True(!string.IsNullOrEmpty(accesstoken));

        this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accesstoken);
    }

    protected async Task<string> GetUserTokenByPassword()
    {
        IPublicClientApplication app = PublicClientApplicationBuilder.Create(this.configuration[ClientIdSettingName])
                                                                     .WithAuthority(this.configuration[AuthoritySettingName])
                                                                     .Build();

        string? scopeString = this.configuration[ScopesSettingName];
        Assert.NotNull(scopeString);

        string[] scopes = scopeString.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var accounts = await app.GetAccountsAsync();

        AuthenticationResult? result = null;

        if (accounts.Any())
        {
            result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
        }
        else
        {
            result = await app.AcquireTokenByUsernamePassword(scopes, this.configuration[UsernameSettingName], this.configuration[PasswordSettingName]).ExecuteAsync();
        }

        return result?.AccessToken ?? string.Empty;
    }
}
