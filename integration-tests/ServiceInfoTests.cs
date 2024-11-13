// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Xunit;

namespace ChatCopilotIntegrationTests;

public class ServiceInfoTests : ChatCopilotIntegrationTest
{
    private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async void GetServiceInfo()
    {
        await this.SetUpAuthAsync();

        HttpResponseMessage response = await this.HTTPClient.GetAsync("info/");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var objectFromResponse = await JsonSerializer.DeserializeAsync<ServiceInfoResponse>(contentStream, jsonOptions);

        Assert.NotNull(objectFromResponse);
        Assert.False(string.IsNullOrEmpty(objectFromResponse.MemoryStore.SelectedType));
        Assert.False(string.IsNullOrEmpty(objectFromResponse.Version));
    }

    [Fact]
    public async void GetAuthConfig()
    {
        HttpResponseMessage response = await this.HTTPClient.GetAsync("authConfig/");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var objectFromResponse = await JsonSerializer.DeserializeAsync<FrontendAuthConfig>(contentStream, jsonOptions);

        Assert.NotNull(objectFromResponse);
        Assert.Equal(ChatAuthenticationOptions.AuthenticationType.AzureAd.ToString(), objectFromResponse.AuthType);
        Assert.Equal(this.Configuration[AuthoritySettingName], objectFromResponse.AadAuthority);
        Assert.Equal(this.Configuration[ClientIdSettingName], objectFromResponse.AadClientId);
        Assert.False(string.IsNullOrEmpty(objectFromResponse.AadApiScope));
    }
}
