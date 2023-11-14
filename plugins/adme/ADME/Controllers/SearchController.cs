using ADME.Models;
using ADME.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ADME.Controllers;

// [Authorize]
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ICognitiveSearchService _cognitiveSearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ICognitiveSearchService cognitiveSearchService, ILogger<SearchController> logger)
    {
        _cognitiveSearchService = cognitiveSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Proxy function for SearchClient.Search
    /// </summary>
    /// <param name="parm"></param>
    /// <param name="filter"></param>
    /// <param name="cts"></param>
    /// <returns></returns>
    [HttpGet("{parm}", Name = "Search")]
    public async Task<ActionResult<SearchResult>> Search(string parm,
        [FromQuery] SearchFilters filter, [FromQuery] CancellationToken cts)
    {
        _logger.LogInformation("Searching by {Param}", parm);
        var result = await _cognitiveSearchService.SearchAsync<SearchResult>(parm, filter, cts);
        return Ok(result);
    }

    /// <summary>
    /// Proxy function for SearchClient.GetDocument
    /// </summary>
    /// <param name="key">Use encoded metadata_storage_path</param>
    /// <returns></returns>
    [HttpGet("get_by_key", Name = "GetDocumentByKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<SearchResult>> GetByKey([FromQuery] string key)
    {
        _logger.LogInformation("Searching document by key: {Key}", key);
        var result = await _cognitiveSearchService.GetDocumentInfoByKeyAsync<SearchResult>(key);

        if (result == null)
            return NotFound();
        return Ok(result);
    }
}