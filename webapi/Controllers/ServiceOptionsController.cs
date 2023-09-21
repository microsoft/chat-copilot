// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Reflection;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for returning the service options to the client.
/// </summary>
[ApiController]
public class ServiceOptionsController : ControllerBase
{
    private readonly ILogger<ServiceOptionsController> _logger;

    private readonly IConfiguration Configuration;

    private readonly SemanticMemoryConfig memoryOptions;

    public ServiceOptionsController(
        ILogger<ServiceOptionsController> logger,
        IConfiguration configuration,
        IOptions<SemanticMemoryConfig> memoryOptions)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this.memoryOptions = memoryOptions.Value;
    }

    /// <summary>
    /// Return the memory store type that is configured.
    /// </summary>
    [Route("serviceOptions")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetServiceOptions()
    {
        var response = new ServiceOptionsResponse()
        {
            MemoryStore = new MemoryStoreOptionResponse()
            {
                Types = Enum.GetNames(typeof(MemoryStoreType)),
                SelectedType = this.memoryOptions.GetMemoryStoreType(this.Configuration).ToString(),
            },
            Version = GetAssemblyFileVersion()
        };

        return this.Ok(response);
    }

    private static string GetAssemblyFileVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

        return fileVersion.FileVersion ?? string.Empty;
    }
}
