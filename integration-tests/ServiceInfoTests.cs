// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using System.Text.Json;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Xunit;

namespace ChatCopilotIntegrationTests;

public class ServiceInfoTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async void GetServiceInfo()
    {
        await this.SetUpAuth();

        HttpResponseMessage response = await this._httpClient.GetAsync("info/");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var objectFromResponse = await JsonSerializer.DeserializeAsync<ServiceInfoResponse>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(objectFromResponse);
        Assert.False(string.IsNullOrEmpty(objectFromResponse.MemoryStore.SelectedType));
        Assert.False(string.IsNullOrEmpty(objectFromResponse.Version));
    }

    [Fact]
    public async void GetAuthConfig()
    {
        HttpResponseMessage response = await this._httpClient.GetAsync("authConfig/");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var objectFromResponse = await JsonSerializer.DeserializeAsync<FrontendAuthConfig>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(objectFromResponse);
        Assert.Equal(ChatAuthenticationOptions.AuthenticationType.AzureAd.ToString(), objectFromResponse.AuthType);
        Assert.Equal(this.configuration[AuthoritySettingName], objectFromResponse.AadAuthority);
        Assert.Equal(this.configuration[ClientIdSettingName], objectFromResponse.AadClientId);
        Assert.False(string.IsNullOrEmpty(objectFromResponse.AadApiScope));
    }
}

