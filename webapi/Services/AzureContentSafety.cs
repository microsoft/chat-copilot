// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.Diagnostics;

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
public sealed class AzureContentSafety : IContentSafetyService
{
    private const string HttpUserAgent = "Copilot Chat";

    private readonly string _endpoint;
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler? _httpClientHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContentSafety"/> class.
    /// </summary>
    /// <param name="endpoint">Endpoint for service API call.</param>
    /// <param name="apiKey">The API key.</param>
    /// <param name="httpClientHandler">Instance of <see cref="HttpClientHandler"/> to setup specific scenarios.</param>
    public AzureContentSafety(string endpoint, string apiKey, HttpClientHandler httpClientHandler)
    {
        this._endpoint = endpoint;
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
    public AzureContentSafety(string endpoint, string apiKey)
    {
        this._endpoint = endpoint;

        this._httpClientHandler = new() { CheckCertificateRevocationList = true };
        this._httpClient = new(this._httpClientHandler);

        this._httpClient.DefaultRequestHeaders.Add("User-Agent", HttpUserAgent);

        // Subscription Key header required to authenticate requests to Azure API Management (APIM) service
        this._httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
    }

    /// <inheritdoc/>
    public List<string> ParseViolatedCategories(ImageAnalysisResponse imageAnalysisResponse, short threshold)
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

    /// <inheritdoc/>
    public async Task<ImageAnalysisResponse> ImageAnalysisAsync(IFormFile formFile, CancellationToken cancellationToken)
    {
        // Convert the form file to a base64 string
        var base64Image = await this.ConvertFormFileToBase64Async(formFile);
        var image = base64Image.Replace("data:image/png;base64,", "", StringComparison.InvariantCultureIgnoreCase).Replace("data:image/jpeg;base64,", "", StringComparison.InvariantCultureIgnoreCase);
        ImageContent content = new(image);
        ImageAnalysisRequest requestBody = new(content);

        using var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{this._endpoint}/contentsafety/image:analyze?api-version=2023-04-30-preview"),
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
        };

        var response = await this._httpClient.SendAsync(httpRequestMessage, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || body is null)
        {
            throw new SKException($"[Content Safety] Failed to analyze image. {response.StatusCode}");
        }

        var result = JsonSerializer.Deserialize<ImageAnalysisResponse>(body!);
        if (result is null)
        {
            throw new SKException($"[Content Safety] Failed to analyze image. Details: {body}");
        }
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._httpClient.Dispose();
        this._httpClientHandler?.Dispose();
    }

    #region Private Methods

    /// <summary>
    /// Helper method to convert a form file to a base64 string.
    /// </summary>
    /// <param name="file">An IFormFile object.</param>
    /// <returns>A Base64 string of the content of the image.</returns>
    private async Task<string> ConvertFormFileToBase64Async(IFormFile formFile)
    {
        using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();
        return Convert.ToBase64String(bytes);
    }

    #endregion
}
