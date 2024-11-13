﻿// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;

[ApiController]
public class SpeechTokenController : ControllerBase
{
    private sealed class TokenResult
    {
        public string? Token { get; set; }
        public HttpStatusCode? ResponseCode { get; set; }
    }

    private readonly ILogger<SpeechTokenController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureSpeechOptions _options;

    public SpeechTokenController(IOptions<AzureSpeechOptions> options,
        ILogger<SpeechTokenController> logger,
        IHttpClientFactory httpClientFactory)
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._options = options.Value;
    }

    /// <summary>
    /// Get an authorization token and region
    /// </summary>
    [Route("speechToken")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SpeechTokenResponse>> GetAsync()
    {
        // Azure Speech token support is optional. If the configuration is missing or incomplete, return an unsuccessful token response.
        if (string.IsNullOrWhiteSpace(this._options.Region) ||
            string.IsNullOrWhiteSpace(this._options.Key))
        {
            return new SpeechTokenResponse { IsSuccess = false };
        }

        string fetchTokenUri = "https://" + this._options.Region + ".api.cognitive.microsoft.com/sts/v1.0/issueToken";

        TokenResult tokenResult = await this.FetchTokenAsync(fetchTokenUri, this._options.Key);
        var isSuccess = tokenResult.ResponseCode != HttpStatusCode.NotFound;
        return new SpeechTokenResponse { Token = tokenResult.Token, Region = this._options.Region, IsSuccess = isSuccess };
    }

    private async Task<TokenResult> FetchTokenAsync(string fetchUri, string subscriptionKey)
    {
        using var client = this._httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, fetchUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

        var result = await client.SendAsync(request);
        if (result.IsSuccessStatusCode)
        {
            var response = result.EnsureSuccessStatusCode();
            this._logger.LogDebug("Token Uri: {0}", fetchUri);
            string token = await result.Content.ReadAsStringAsync();
            return new TokenResult { Token = token, ResponseCode = response.StatusCode };
        }

        return new TokenResult { ResponseCode = HttpStatusCode.NotFound };
    }
}
