// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using CopilotChat.WebApi.Controllers;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// $$$
/// </summary>
public class ChatMigrationMiddleware
{
    internal const string GlobalChatMigrationActiveCall = "GlobalChatMigrationActive";

    private readonly RequestDelegate _next;
    private readonly IOptions<DocumentMemoryOptions> _documentMemoryOptions;
    private readonly IOptions<PromptsOptions> _promptOptions;
    private readonly ChatMemorySourceRepository _sourceRepository;
    private readonly IChatMigrationMonitor _migrationMonitor;
    private readonly IChatMemoryMigrationService _migrationService;
    private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;
    private readonly ILogger<ChatMigrationController> _logger;

    public ChatMigrationMiddleware(
        RequestDelegate next,
        IOptions<DocumentMemoryOptions> documentMemoryOptions,
        IOptions<PromptsOptions> promptOptions,
        ChatMemorySourceRepository sourceRepository,
        IChatMigrationMonitor migrationMonitor,
        IChatMemoryMigrationService migrationService,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        ILogger<ChatMigrationController> logger)

    {
        this._next = next;
        this._documentMemoryOptions = documentMemoryOptions;
        this._promptOptions = promptOptions;
        this._sourceRepository = sourceRepository;
        this._migrationMonitor = migrationMonitor;
        this._migrationService = migrationService;
        this._messageRelayHubContext = messageRelayHubContext;
        this._logger = logger;
    }

    public async Task Invoke(HttpContext ctx, IKernel kernel)
    {
        // Monitor caches status for minimum middle-ware impact
        var migrationStatus = await this._migrationMonitor.GetCurrentStatusAsync(kernel.Memory).ConfigureAwait(false);

        if (migrationStatus != ChatMigrationStatus.None)
        {
            await this._messageRelayHubContext.Clients.All.SendAsync(GlobalChatMigrationActiveCall, "Chat migration in progress").ConfigureAwait(false);
        }

        if (migrationStatus == ChatMigrationStatus.RequiresUpgrade)
        {
            // $$$ BACKGROUND JOB
            try
            {
                // All documents will need to be re-imported
                await this.RemoveMemorySourcesAsync();

                // Migrate all chats to single index
                await this._migrationService.MigrateAsync(kernel.Memory);
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                this._logger.LogError(ex, "Error migrating chat memories");
                //return this.Problem(ex.Message); $$$
            }
        }

        await this._next(ctx);
    }

    private async Task RemoveMemorySourcesAsync()
    {
        var documentMemories = await this._sourceRepository.GetAllAsync().ConfigureAwait(false);
        foreach (var document in documentMemories) // $$$ PARALLEL ???
        {
            await this._sourceRepository.DeleteAsync(document).ConfigureAwait(false);
        }
    }
}
