// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using CopilotChat.WebApi.Models.Response;
using Xunit;

namespace ChatCopilotIntegrationTests;

public class SpeechTokenTests : ChatCopilotIntegrationTest
{
    private static readonly JsonSerializerOptions jsonOpts = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async void GetSpeechToken()
    {
        await this.SetUpAuthAsync();

        HttpResponseMessage response = await this.HTTPClient.GetAsync("speechToken/");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var speechTokenResponse = await JsonSerializer.DeserializeAsync<SpeechTokenResponse>(contentStream, jsonOpts);

        Assert.NotNull(speechTokenResponse);
        Assert.True((speechTokenResponse.IsSuccess == true && !string.IsNullOrEmpty(speechTokenResponse.Token)) ||
                    speechTokenResponse.IsSuccess == false);
    }
}
