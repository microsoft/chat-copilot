// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI;

namespace CopilotChat.WebApi.Services;

public record AnalysisResult(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("severity")] short Severity
);

public record ImageContent([property: JsonPropertyName("content")] string Content);

public record ImageAnalysisRequest(
    [property: JsonPropertyName("image")] ImageContent Image
);

/// <summary>
/// Moderator service to handle content safety.
/// </summary>
public sealed class AzureContentSafety : IDisposable
{
    private const string HttpUserAgent = "Copilot Chat";

    private readonly Uri _endpoint;
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler? _httpClientHandler;

    /// <summary>
    /// Options for the content safety.
    /// </summary>
    private readonly ContentSafetyOptions _contentSafetyOptions;

    /// <summary>
    /// Gets the options for the content safety.
    /// </summary>
    public ContentSafetyOptions Options => this._contentSafetyOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContentSafety"/> class.
    /// </summary>
    /// <param name="endpoint">Endpoint for service API call.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="contentSafetyOptions">Content safety options from appsettings.</param>
    /// <param name="httpClientHandler">Instance of <see cref="HttpClientHandler"/> to setup specific scenarios.</param>
    public AzureContentSafety(Uri endpoint, string apiKey, ContentSafetyOptions contentSafetyOptions, HttpClientHandler httpClientHandler)
    {
        this._endpoint = endpoint;
        this._contentSafetyOptions = contentSafetyOptions;
        this._httpClient = new(httpClientHandler);

        this._httpClient.DefaultRequestHeaders.Add("User-Agent", HttpUserAgent);

        // Subscription Key header required to authenticate requests to Azure API Management (APIM) service
        this._httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContentSafety"/> class.
    /// </summary>
    /// <param name="endpoint">Endpoint for service API call.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="contentSafetyOptions">Content safety options from appsettings.</param>
    public AzureContentSafety(Uri endpoint, string apiKey, ContentSafetyOptions contentSafetyOptions)
    {
        this._endpoint = endpoint;
        this._contentSafetyOptions = contentSafetyOptions;

        this._httpClientHandler = new() { CheckCertificateRevocationList = true };
        this._httpClient = new(this._httpClientHandler);

        this._httpClient.DefaultRequestHeaders.Add("User-Agent", HttpUserAgent);

        // Subscription Key header required to authenticate requests to Azure API Management (APIM) service
        this._httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
    }

    /// <summary>
    /// Checks the state of the content safety.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <returns>True if content safety is enabled with non-null endpoint.</returns>
    public bool ContentSafetyStatus(ILogger logger)
    {
        if (this._endpoint is null)
        {
            logger.LogWarning("Content Safety is missing a valid endpoint. Please check the configuration.");
            return false;
        }

        return this._contentSafetyOptions.Enabled;
    }

    /// <summary>
    /// Parse the analysis result and return the violated categories.
    /// </summary>
    /// <param name="imageAnalysisResponse">The content analysis result.</param>
    /// <param name="threshold">The violation threshold.</param>
    /// <returns>The list of violated category names. Will return an empty list if there is no violation.</returns>
    public static List<string> ParseViolatedCategories(ImageAnalysisResponse imageAnalysisResponse, short threshold)
    {
        var violatedCategories = new List<string>();

        foreach (var property in typeof(ImageAnalysisResponse).GetProperties())
        {
            var analysisResult = property.GetValue(imageAnalysisResponse) as AnalysisResult;
            if (analysisResult != null && analysisResult.Severity >= threshold)
            {
                violatedCategories.Add($"{analysisResult.Category} ({analysisResult.Severity})");
            }
        }

        return violatedCategories;
    }

    /// <summary>
    /// Invokes a sync API to perform harmful content analysis on image.
    /// <param name="base64Image">Base64 envoding content of image</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// </summary>
    /// <returns>SKContext containing the image analysis result.</returns>
    public async Task<ImageAnalysisResponse> ImageAnalysisAsync(string base64Image, CancellationToken cancellationToken)
    {
        var image = base64Image.Replace("data:image/png;base64,", "", StringComparison.InvariantCultureIgnoreCase).Replace("data:image/jpeg;base64,", "", StringComparison.InvariantCultureIgnoreCase);
        ImageContent content = new(image);
        ImageAnalysisRequest requestBody = new(content);

        using var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{this._endpoint}/contentsafety/image:analyze?api-version=2023-04-30-preview"),
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };

        var response = await this._httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode || body is null)
        {
            throw new AIException(
                response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? AIException.ErrorCodes.AccessDenied : AIException.ErrorCodes.UnknownError,
                $"[Content Safety] Failed to analyze image. {response.StatusCode}");
        }

        var result = JsonSerializer.Deserialize<ImageAnalysisResponse>(body!);
        if (result is null)
        {
            throw new AIException(
                AIException.ErrorCodes.UnknownError,
                $"[Content Safety] Failed to analyze image. {body}");
        }
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._httpClient.Dispose();
        this._httpClientHandler?.Dispose();
    }
}
