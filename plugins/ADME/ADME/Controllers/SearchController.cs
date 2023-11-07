using ADME.Models;
using ADME.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace ADME.Controllers;

//[Authorize]
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

    [HttpGet("search/{param}", Name = "Search")]
    public async Task<ActionResult<SearchResult>> Search(string param,
        [FromQuery] SearchFilters filter, [FromQuery] CancellationToken cts)
    {
        _logger.LogInformation("Searching by {Param}", param);
        var result = await _cognitiveSearchService.SearchAsync<SearchResult>(param, filter, cts);
        return Ok(result);
    }

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