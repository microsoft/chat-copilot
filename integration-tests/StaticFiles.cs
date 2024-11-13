// Copyright (c) Microsoft. All rights reserved.

using Xunit;

namespace ChatCopilotIntegrationTests;

public class StaticFiles : ChatCopilotIntegrationTest
{
    [Fact]
    public async void GetStaticFiles()
    {
        HttpResponseMessage response = await this.HTTPClient.GetAsync("index.html");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Content.Headers.ContentLength > 1);

        response = await this.HTTPClient.GetAsync("favicon.ico");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Content.Headers.ContentLength > 1);
    }
}
