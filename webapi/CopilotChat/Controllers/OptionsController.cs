// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernel.Service.Options;

namespace SemanticKernel.Service.CopilotChat.Controllers;

/// <summary>
/// Controller responsible for returning the service options to the client.
/// </summary>
[ApiController]
[Authorize]
public class OptionsController : ControllerBase
{
    private readonly ILogger<OptionsController> _logger;

    private readonly MemoriesStoreOptions _memoriesStoreOptions;

    public OptionsController(
        ILogger<OptionsController> logger,
        IOptions<MemoriesStoreOptions> memoriesStoreOptions)
    {
        this._logger = logger;
        this._memoriesStoreOptions = memoriesStoreOptions.Value;
    }

    /// <summary>
    /// Return the memory store type that is configured.
    /// </summary>
    [Route("memoryStoreType")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetMemoryStoreType()
    {
        return this.Ok(this._memoriesStoreOptions.Type.ToString());
    }
}
