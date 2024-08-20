// Copyright (c) Quartech. All rights reserved.

using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for handling search.
/// </summary>
[ApiController]
public class SearchController : ControllerBase
{
    private readonly ILogger<SearchController> _logger;

    private readonly QSearchService _qSearchService;

    public SearchController(
    ILogger<SearchController> logger,
    SpecializationSourceRepository specializationSourceRepository,
    IOptions<QAzureOpenAIChatOptions> specializationOptions)
    {
        this._logger = logger;
        this._qSearchService = new QSearchService(specializationOptions.Value, specializationSourceRepository);
    }

    /// <summary>
    /// Invokes the Azure search function to get a results.
    /// </summary>
    /// <param name="search">Search Input Text</param>
    /// <param name="specializationKey">specialization Key.</param>
    /// <returns>Results containing the response from search endpoint.</returns>
    [HttpPost]
    [Route("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<QSearchResult> GetMatchesAsync(
        [FromBody] QSearchParameters searchParameters)
    {
        //Scope: To implement filter to give more refined search functionality.      
        var response = await this._qSearchService.GetMatchesAsync(searchParameters);
        return response;
    }
}
