// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernel.Service.CopilotChat.Models;
using SemanticKernel.Service.Options;
using static SemanticKernel.Service.CopilotChat.Models.ServiceOptionsResponse;

namespace SemanticKernel.Service.CopilotChat.Controllers;

/// <summary>
/// Controller responsible for returning the service options to the client.
/// </summary>
[ApiController]
[Authorize]
public class ServiceOptionsController : ControllerBase
{
    private readonly ILogger<ServiceOptionsController> _logger;

    private readonly MemoriesStoreOptions _memoriesStoreOptions;

    public ServiceOptionsController(
        ILogger<ServiceOptionsController> logger,
        IOptions<MemoriesStoreOptions> memoriesStoreOptions)
    {
        this._logger = logger;
        this._memoriesStoreOptions = memoriesStoreOptions.Value;
    }

    // TODO: [Issue #95] Include all service options in a single response.
    /// <summary>
    /// Return the memory store type that is configured.
    /// </summary>
    [Route("serviceOptions")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetServiceOptions()
    {
        return this.Ok(
            new ServiceOptionsResponse()
            {
                MemoriesStoreType = new MemoriesStoreTypeResponse()
                {
                    Types = Enum.GetNames(typeof(MemoriesStoreOptions.MemoriesStoreType)),
                    SelectedType = this._memoriesStoreOptions.Type.ToString()
                }
            }
        );
    }
}
