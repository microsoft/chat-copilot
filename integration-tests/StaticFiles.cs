// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using Xunit;

namespace ChatCopilotIntegrationTests;

public class StaticFiles : ChatCopilotIntegrationTest
{
    [Fact]
    public async void GetStaticFiles()
    {
        HttpResponseMessage response = await this._httpClient.GetAsync("index.html");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Content.Headers.ContentLength > 1);

        response = await this._httpClient.GetAsync("favicon.ico");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Content.Headers.ContentLength > 1);
    }
}
