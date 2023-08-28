// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
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
        HttpResponseMessage response = await this._httpClient.GetAsync("healthz");

        response.EnsureSuccessStatusCode();
    }
}
