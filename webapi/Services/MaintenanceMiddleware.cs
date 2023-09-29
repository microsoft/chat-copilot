// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Controllers;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Middleware for determining is site is undergoing maintenance.
/// </summary>
public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyList<IMaintenanceAction> _actions;
    private readonly IOptions<ServiceOptions> _serviceOptions;
    private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    private bool? _isInMaintenance;

    public MaintenanceMiddleware(
        RequestDelegate next,
        IReadOnlyList<IMaintenanceAction> actions,
        IOptions<ServiceOptions> serviceOptions,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        ILogger<MaintenanceMiddleware> logger)

    {
        this._next = next;
        this._actions = actions;
        this._serviceOptions = serviceOptions;
        this._messageRelayHubContext = messageRelayHubContext;
        this._logger = logger;
    }

    public async Task Invoke(HttpContext ctx, IKernel kernel)
    {
        // Skip inspection if _isInMaintenance explicitly false.
        if (this._isInMaintenance == null || this._isInMaintenance.Value)
        {
            // Maintenance never false => true; always true => false or just false;
            this._isInMaintenance = await this.InspectMaintenanceActionAsync();
        }

        // In maintenance if actions say so or explicitly configured.
        if (this._serviceOptions.Value.InMaintenance)
        {
            await this._messageRelayHubContext.Clients.All.SendAsync(MaintenanceController.GlobalSiteMaintenance, "Site undergoing maintenance...");
        }

        await this._next(ctx);
    }

    private async Task<bool> InspectMaintenanceActionAsync()
    {
        bool inMaintenance = false;

        foreach (var action in this._actions)
        {
            inMaintenance |= await action.InvokeAsync();
        }

        return inMaintenance;
    }
}
