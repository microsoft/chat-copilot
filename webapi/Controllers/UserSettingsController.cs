// Copyright (c) Quartech. All rights reserved.

using System.Net.Http;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for handling loading and updating chat users settings
/// </summary>
[ApiController]
public class UserSettingsController : ControllerBase
{
    private readonly ILogger<UserSettingsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<QAzureOpenAIChatOptions> _chatOptions;

    public UserSettingsController(
        ILogger<UserSettingsController> logger,
        IOptions<QAzureOpenAIChatOptions> chatOptions,
        IHttpClientFactory httpClientFactory
    )
    {
        this._logger = logger;
        this._httpClientFactory = httpClientFactory;
        this._chatOptions = chatOptions;
    }

    /// <summary>
    /// Returns the users settings
    /// </summary>
    /// <param name="chatUserRepository">The injected chat user repository</param>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <returns>Results containing the response from the model.</returns>
    [Route("user-settings")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> LoadSettings(
        [FromServices] ChatUserRepository chatUserRepository,
        [FromServices] IAuthInfo authInfo
    )
    {
        this._logger.LogDebug("Settings request received.");

        var userId = authInfo.UserId;
        ChatUser? user = null;
        await chatUserRepository.TryFindByIdAsync(userId, callback: u => user = u);
        if (user == null)
        {
            user = new ChatUser(userId);
            await chatUserRepository.CreateAsync(user);
        }

        return this.Ok(
            new LoadSettingsResponse
            {
                settings = user.settings,
                adminGroupId = this._chatOptions.Value.AdminGroupMembershipId,
            }
        );
    }

    /// <summary>
    /// Updates a User Setting
    /// </summary>
    /// <param name="chatUserRepository">The injected chat user repository</param>
    /// <param name="authInfo">Auth info for the current request.</param>
    /// <returns>Results containing the response from the model.</returns>
    [Route("user-settings")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> UpdateSetting(
        [FromServices] ChatUserRepository chatUserRepository,
        [FromServices] IAuthInfo authInfo,
        [FromBody] UpdateSettings request
    )
    {
        this._logger.LogDebug("Settings update received.");

        var userId = authInfo.UserId;
        ChatUser? user = null;
        await chatUserRepository.TryFindByIdAsync(userId, callback: u => user = u);
        if (user == null)
        {
            return this.BadRequest("Chat user does not exist.");
        }
        ;

        if (request.Setting == "darkMode")
        {
            user.settings.darkMode = request.Enabled;
        }
        if (request.Setting == "pluginsPersonas")
        {
            user.settings.pluginsPersonas = request.Enabled;
        }
        if (request.Setting == "simplifiedChat")
        {
            user.settings.simplifiedChat = request.Enabled;
        }
        await chatUserRepository.UpsertAsync(user);

        return this.Ok(user.settings);
    }
}
