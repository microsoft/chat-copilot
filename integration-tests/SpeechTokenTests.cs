// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using System.Text.Json;
using CopilotChat.WebApi.Models.Response;
using Xunit;

namespace ChatCopilotIntegrationTests;

public class SpeechTokenTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async void GetSpeechToken()
    {
        await this.SetUpAuth();

        HttpResponseMessage response = await this._httpClient.GetAsync("speechToken/");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var speechTokenResponse = await JsonSerializer.DeserializeAsync<SpeechTokenResponse>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(speechTokenResponse);
        Assert.True((speechTokenResponse.IsSuccess == true && !string.IsNullOrEmpty(speechTokenResponse.Token)) ||
                     speechTokenResponse.IsSuccess == false);
    }
}
