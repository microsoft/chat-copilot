// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for returning the service options to the client.
/// </summary>
[ApiController]
public class PluginController : ControllerBase
{
    private const string PluginStateChanged = "PluginStateChanged";
    private readonly ILogger<PluginController> _logger;
    private readonly IDictionary<string, Plugin> _availablePlugins;
    private readonly ChatSessionRepository _sessionRepository;

    public PluginController(
        ILogger<PluginController> logger,
        IDictionary<string, Plugin> availablePlugins,
        ChatSessionRepository sessionRepository)
    {
        this._logger = logger;
        this._availablePlugins = availablePlugins;
        this._sessionRepository = sessionRepository;
    }

    /// <summary>
    /// Enable or disable a plugin for a chat session.
    /// </summary>
    [HttpPut]
    [ActionName("SetPluginStateAsync")]
    [Route("chatSession/pluginState/{chatId:guid}/{pluginName}/{enabled:bool}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> SetPluginStateAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        Guid chatId,
        string pluginName,
        bool enabled)
    {
        if (!this._availablePlugins.ContainsKey(pluginName))
        {
            return this.NotFound("Plugin not found.");
        }

        var chatIdString = chatId.ToString();
        ChatSession? chat = null;
        if (!(await this._sessionRepository.TryFindByIdAsync(chatIdString, callback: v => chat = v)) || chat == null)
        {
            return this.NotFound("Chat not found.");
        }

        if (enabled)
        {
            chat.EnabledPlugins.Add(pluginName);
        }
        else
        {
            chat.EnabledPlugins.Remove(pluginName);
        }

        await this._sessionRepository.UpsertAsync(chat);
        await messageRelayHubContext.Clients.Group(chatIdString).SendAsync(PluginStateChanged, chatIdString, pluginName, enabled);

        return this.NoContent();
    }
}
