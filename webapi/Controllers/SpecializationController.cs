// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using CopilotChat.WebApi.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for managing specializations.
/// </summary>
[ApiController]
public class SpecializationController : ControllerBase
{
    private readonly ILogger<SpecializationController> _logger;

    private readonly QSpecializationService _qspecializationService;

    private readonly QAzureOpenAIChatExtension _qAzureOpenAIChatExtension;

    private readonly QAzureOpenAIChatOptions _qAzureOpenAIChatOptions;

    private readonly PromptsOptions _promptOptions;

    public SpecializationController(
        ILogger<SpecializationController> logger,
        IOptions<QAzureOpenAIChatOptions> specializationOptions,
        SpecializationRepository specializationSourceRepository,
        IOptions<PromptsOptions> promptsOptions
    )
    {
        this._logger = logger;
        this._qAzureOpenAIChatOptions = specializationOptions.Value;
        this._qAzureOpenAIChatExtension = new QAzureOpenAIChatExtension(
            specializationOptions.Value,
            specializationSourceRepository
        );
        this._qspecializationService = new QSpecializationService(
            specializationSourceRepository,
            specializationOptions.Value
        );
        this._promptOptions = promptsOptions.Value;
    }

    /// <summary>
    /// Get all available specializations maintained in the system.
    /// </summary>
    /// <returns>A list of available specializations. An empty list if no specializations are found.</returns>
    [HttpGet]
    [Route("specializations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<OkObjectResult> GetAllSpecializations()
    {
        var specializationResponses = new List<QSpecializationResponse>();
        IEnumerable<Specialization> specializations = await this._qspecializationService.GetAllSpecializations();
        foreach (Specialization specialization in specializations)
        {
            QSpecializationResponse qSpecializationResponse = new(specialization);
            specializationResponses.Add(qSpecializationResponse);
        }
        var defaultSpecialization = this.GetDefaultSpecialization();
        specializationResponses.Add(new QSpecializationResponse(defaultSpecialization));
        return this.Ok(specializationResponses);
    }

    /// <summary>
    /// Get all available specialization indexes maintained in the system.
    /// </summary>
    /// <returns>A list of available specialization indexes. An empty list if no specialization indexes are found.</returns>
    [HttpGet]
    [Route("specialization/indexes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public List<string> GetAllSpecializationIndexes()
    {
        return this._qAzureOpenAIChatExtension.GetAllSpecializationIndexNames();
    }

    /// <summary>
    /// Get all chat completion deployments.
    /// </summary>
    /// <returns>A list of chat completion deployments.</returns>
    [HttpGet]
    [Route("specialization/deployments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public List<string> GetAllChatCompletionDeployments()
    {
        return this._qAzureOpenAIChatExtension.GetAllChatCompletionDeployments();
    }

    /// <summary>
    /// Creates a new specialization.
    /// </summary>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <param name="qSpecializationParameters">Contains the specialization parameters</param>
    /// <returns>The HTTP action result.</returns>
    [Route("specializations")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> CreateSpecializationAsync(
        [FromServices] IAuthInfo authInfo,
        [FromForm] QSpecializationMutate qSpecializationMutate
    )
    {
        try
        {
            var _specializationsource = await this._qspecializationService.SaveSpecialization(qSpecializationMutate);

            QSpecializationResponse qSpecializationResponse = new(_specializationsource);
            return this.Ok(qSpecializationResponse);
        }
        catch (Azure.RequestFailedException ex)
        {
            this._logger.LogError(ex, "Specialization create threw an exception");

            return this.StatusCode(500, $"Failed to create specialization for label '{qSpecializationMutate.Label}'.");
        }
    }

    /// <summary>
    /// Edit a specialization.
    /// </summary>
    /// <param name="qSpecializationParameters">Contains the specialization parameters</param>
    /// <param name="specializationId">The specializtion id.</param>
    /// <returns>The HTTP action result.</returns>
    [HttpPatch]
    [Route("specializations/{specializationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditSpecializationAsync(
        [FromForm] QSpecializationMutate qSpecializationMutate,
        [FromRoute] Guid specializationId
    )
    {
        try
        {
            Specialization? specializationToEdit = await this._qspecializationService.UpdateSpecialization(
                specializationId,
                qSpecializationMutate
            );

            if (specializationToEdit != null)
            {
                QSpecializationResponse qSpecializationResponse = new(specializationToEdit);
                return this.Ok(qSpecializationResponse);
            }

            return this.StatusCode(500, $"Failed to update specialization for id '{specializationId}'.");
        }
        catch (Azure.RequestFailedException ex)
        {
            this._logger.LogError(ex, "Specialization update threw an exception");

            return this.StatusCode(500, $"Failed to edit specialization for id '{specializationId}'.");
        }
    }

    /// <summary>
    /// Delete specialization.
    /// </summary>
    /// <param name="specializationId">The specializtion id.</param>
    /// <returns>The HTTP action result.</returns>
    [HttpDelete]
    [Route("specializations/{specializationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DisableSpecializationAsync(Guid specializationId)
    {
        try
        {
            Specialization specialization = await this._qspecializationService.GetSpecializationAsync(
                specializationId.ToString()
            );

            bool result = await this._qspecializationService.DeleteSpecialization(specializationId);

            if (result)
            {
                return this.Ok(specializationId);
            }

            return this.StatusCode(500, $"Failed to delete specialization for id '{specializationId}'.");
        }
        catch (Azure.RequestFailedException ex)
        {
            this._logger.LogError(ex, "Specialization delete threw an exception");

            return this.StatusCode(500, $"Failed to delete specialization for id '{specializationId}'.");
        }
    }

    /// <summary>
    /// Gets the default specialization
    /// </summary>
    /// <returns>An instance of <see cref="Specialization"/></returns>
    private Specialization GetDefaultSpecialization()
    {
        var defaultSpecialization = new Specialization()
        {
            Id = "general",
            Label = "general",
            Name = "General",
            Description = string.IsNullOrEmpty(this._qAzureOpenAIChatOptions.DefaultSpecializationDescription)
                ? "This is a chat between an intelligent AI bot named Copilot and one or more participants. SK stands for Semantic Kernel, the AI platform used to build the bot."
                : this._qAzureOpenAIChatOptions.DefaultSpecializationDescription,
            RoleInformation = this._promptOptions.SystemDescription,
            ImageFilePath = ResourceUtils.GetImageAsDataUri(this._qAzureOpenAIChatOptions.DefaultSpecializationImage),
            IconFilePath = ResourceUtils.GetImageAsDataUri(this._qAzureOpenAIChatOptions.DefaultSpecializationIcon),
            Deployment = "gpt-4o",
            RestrictResultScope = this._qAzureOpenAIChatOptions.DefaultRestrictResultScope,
            Strictness = this._qAzureOpenAIChatOptions.DefaultStrictness,
            DocumentCount = this._qAzureOpenAIChatOptions.DefaultDocumentCount,
        };

        return defaultSpecialization;
    }
}
