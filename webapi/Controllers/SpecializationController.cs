// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for handling specializations.
/// </summary>
[ApiController]
public class SpecializationController : ControllerBase
{
    private readonly ILogger<SpecializationController> _logger;

    private readonly QAzureOpenAIChatOptions _qAzureOpenAIChatOptions;

    public SpecializationController(
    ILogger<SpecializationController> logger,
    IOptions<QAzureOpenAIChatOptions> qAzureOpenAIChatOptions)
    {
        this._logger = logger;
        this._qAzureOpenAIChatOptions = qAzureOpenAIChatOptions.Value;
    }

    /// <summary>
    /// Get all available specializations maintained in the system. Return an empty list if no specializations are found.
    /// </summary>
    /// <returns>A list of available specializations. An empty list if no specializations are found.</returns>
    [HttpGet]
    [Route("specializations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public OkObjectResult GetAllSepcializations()
    {
        //This takes data from appsettings.json. Eventually this will be managed in defined data store.  
        //Scope: To manage through admin privileges.      
        var specializations = new List<SpecializationSession>();
        foreach (var _specialization in this._qAzureOpenAIChatOptions.Specializations)
        {
            specializations.Add(new SpecializationSession(_specialization.Key, _specialization.Name, _specialization.Description, _specialization.ImageFilepath, _specialization.GroupMemberships));
        }
        return this.Ok(specializations);
    }
}
