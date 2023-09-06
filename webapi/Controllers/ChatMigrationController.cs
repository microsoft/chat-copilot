// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// $$$
/// </summary>
[ApiController]
public class ChatMigrationController : ControllerBase
{
    private const string GlobalChatMigrationActiveCall = "GlobalChatMigrationActive";
    private const string GlobalChatMigrationCompleteCall = "GlobalChatMigrationComplete";

    private readonly ILogger<ChatMigrationController> _logger;
    private readonly IAuthInfo _authInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMigrationController"/> class.
    /// </summary>
    public ChatMigrationController(
        ILogger<ChatMigrationController> logger,
        IAuthInfo authInfo)
    {
        this._logger = logger;
        this._authInfo = authInfo;
    }

    /// <summary>
    /// $$$
    /// </summary>
    [Route("maintenance/")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatVersionStatus>> MigrateStatusAsync(
        [FromServices] IKernel kernel,
        [FromServices] IChatMigrationMonitor migrationMonitor,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        CancellationToken cancelToken = default)
    {
        var migrationStatus = await migrationMonitor.GetCurrentStatusAsync(kernel.Memory, cancelToken).ConfigureAwait(false);

        if (migrationStatus != ChatVersionStatus.None)
        {
            await messageRelayHubContext.Clients.All.SendAsync(GlobalChatMigrationActiveCall, "Chat migration in progress", cancelToken).ConfigureAwait(false);
        }
        else
        {
            await messageRelayHubContext.Clients.All.SendAsync(GlobalChatMigrationCompleteCall, "Chat migration completed.", cancelToken).ConfigureAwait(false);
        }

        return this.Ok($"Migration status: {migrationStatus.Label}");
    }

    ///// <summary>
    ///// $$$
    ///// </summary>
    //[Route("chats/version/")]
    //[HttpPost]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<ActionResult> MigrateChatsAsync(
    //    [FromServices] IChatMigrationMonitor migrationMonitor,
    //    [FromServices] IChatMemoryMigrationService migrationService,
    //    [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext)
    //{
    //    // $$$ HOOK / ENTRY POINT ???
    //    // Monitor caches status for minimum middle-ware impact
    //    var migrationStatus = await migrationMonitor.GetCurrentStatusAsync().ConfigureAwait(false);

    //    if (migrationStatus != ChatVersionStatus.None)
    //    {
    //        await messageRelayHubContext.Clients.All.SendAsync(GlobalChatMigrationActiveCall, "Chat migration in progress").ConfigureAwait(false);
    //    }

    //    if (migrationStatus == ChatVersionStatus.RequiresUpgrade)
    //    {
    //        try
    //        {
    //            // All documents will need to be re-imported
    //            await this.RemoveMemorySourcesAsync();
    //            // Migrate all chats to single index
    //            await migrationService.MigrateAsync();

    //            await messageRelayHubContext.Clients.All.SendAsync(GlobalChatMigrationCompleteCall, "Chat migration complete").ConfigureAwait(false);
    //        }
    //        catch (Exception ex) when (!ex.IsCriticalException())
    //        {
    //            return this.Problem(ex.Message);
    //        }
    //    }

    //    return this.Ok();
    //}

    //private async Task RemoveMemorySourcesAsync()
    //{
    //    var documentMemories = await this._sourceRepository.GetAllAsync().ConfigureAwait(false);
    //    foreach (var document in documentMemories) // $$$ PARRALLEL ???
    //    {
    //        await this._sourceRepository.DeleteAsync(document).ConfigureAwait(false);
    //    }
    //}
}
