// Copyright (c) Microsoft. All rights reserved.

using Xunit;

namespace ChatCopilotIntegrationTests;

/// <summary>
/// Class for testing the healthcheck endpoint
/// </summary>
public class HealthzTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async void HealthzSuccessfullyReturns()
    {
        HttpResponseMessage response = await this.HTTPClient.GetAsync("healthz");

        response.EnsureSuccessStatusCode();
    }
}
