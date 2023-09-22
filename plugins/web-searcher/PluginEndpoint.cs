// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PluginShared;
using WebSearcher.Models;

namespace WebSearcher;

/// <summary>
/// Plugin endpoints
/// </summary>
public class PluginEndpoint
{
    private readonly PluginConfig _config;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PluginEndpoint(PluginConfig config, ILogger<PluginEndpoint> logger)
    {
        this._config = config;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the plugin manifest.
    /// </summary>
    /// <param name="req">The http request data.</param>
    /// <returns></returns>
    [Function("WellKnownAIPlugin")]
    public async Task<HttpResponseData> WellKnownAIPlugin(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = ".well-known/ai-plugin.json")] HttpRequestData req)
    {
        var pluginManifest = new PluginManifest()
        {
            NameForModel = "WebSearcher",
            NameForHuman = "WebSearcher",
            DescriptionForModel = "Searches the web",
            DescriptionForHuman = "Searches the web",
            Auth = new PluginAuth()
            {
                Type = "user_http"
            },
            Api = new PluginApi()
            {
                Type = "openapi",
                Url = string.Empty
            },
        };
        pluginManifest.Api.Url = $"{req.Url.Scheme}://{req.Url.Host}:{req.Url.Port}/swagger.json";

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(pluginManifest);
        return response;
    }

    [OpenApiOperation(operationId: "Search", tags: new[] { "WebSearchfunction" }, Description = "Searches the web for the given query.")]
    [OpenApiParameter(name: "Query", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The query")]
    [OpenApiParameter(name: "NumResults", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "The maximum number of results to return")]
    [OpenApiParameter(name: "Offset", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "The number of results to skip")]
    [OpenApiParameter(name: "Site", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The specific site to search within")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "Returns a collection of search results with the name, URL and snippet for each.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid query")]
    [Function("WebSearch")]
    public async Task<HttpResponseData> WebSearch([HttpTrigger(AuthorizationLevel.Function, "get", Route = "search")] HttpRequestData req)
    {
        var queries = QueryHelpers.ParseQuery(req.Url.Query);
        var query = queries.ContainsKey("Query") ? queries["Query"].ToString() : string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            return await this.CreateBadRequestResponseAsync(req, "Empty query.");
        }

        var numResults = queries.ContainsKey("NumResults") ? int.Parse(queries["NumResults"]) : 0;
        if (numResults <= 0)
        {
            return await this.CreateBadRequestResponseAsync(req, "Invalid number of results.");
        }

        var offset = queries.ContainsKey("Offset") ? int.Parse(queries["Offset"]) : 0;

        var site = queries.ContainsKey("Site") ? queries["Site"].ToString() : string.Empty;
        if (string.IsNullOrWhiteSpace(site))
        {
            this._logger.LogDebug("Searching the web for '{0}'", query);
        }
        else
        {
            this._logger.LogDebug("Searching the web for '{0}' within '{1}'", query, site);
        }

        using (var httpClient = new HttpClient())
        {
            var queryString = $"?q={Uri.EscapeDataString(query)}";
            queryString += string.IsNullOrWhiteSpace(site) ? string.Empty : $"+site:{site}";
            queryString += $"&count={numResults}";
            queryString += $"&offset={offset}";


            var uri = new Uri($"{this._config.BingApiBaseUrl}{queryString}");
            this._logger.LogDebug("Sending request to {0}", uri);

            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._config.BingApiKey);

            var bingResponse = await httpClient.GetStringAsync(uri);

            this._logger.LogDebug("Search completed. Response: {0}", bingResponse);

            BingSearchResponse? data = JsonSerializer.Deserialize<BingSearchResponse>(bingResponse);
            WebPage[]? results = data?.WebPages?.Value;

            var responseText = results == null
                ? "No results found."
                : string.Join(",",
                    results.Select(r => $"[NAME]{r.Name}[END NAME] [URL]{r.Url}[END URL] [SNIPPET]{r.Snippet}[END SNIPPET]"));

            return await this.CreateOkResponseAsync(req, responseText);
        }
    }

    /// <summary>
    /// Creates an OK response containing texts with the given content.
    /// </summary>
    /// <param name="req">The http request data.</param>
    /// <param name="content">The content.</param>
    /// <returns></returns>
    private async Task<HttpResponseData> CreateOkResponseAsync(HttpRequestData req, string content)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(content);
        return response;
    }

    /// <summary>
    /// Creates a bad request response containing the given error message.
    /// </summary>
    /// <param name="req">The http request data.</param>
    /// <param name="errMsg">The error message.</param>
    /// <returns></returns>
    private async Task<HttpResponseData> CreateBadRequestResponseAsync(HttpRequestData req, string errMsg)
    {
        this._logger.LogError(errMsg);

        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(errMsg);
        return response;
    }
}
