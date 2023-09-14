// Copyright (c) Microsoft. All rights reserved.

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
    private readonly IOptions<ServiceOptions> _serviceOptions;
    private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    public MaintenanceMiddleware(
        RequestDelegate next,
        IOptions<ServiceOptions> servicetOptions,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        ILogger<MaintenanceMiddleware> logger)

    {
        this._next = next;
        this._serviceOptions = servicetOptions;
        this._messageRelayHubContext = messageRelayHubContext;
        this._logger = logger;
    }

    public async Task Invoke(HttpContext ctx, IKernel kernel)
    {
        if (this._serviceOptions.Value.InMaintenance)
        {
            await this._messageRelayHubContext.Clients.All.SendAsync(MaintenanceController.GlobalSiteMaintenance, "Site undergoing maintenance...").ConfigureAwait(false);
        }

        await this._next(ctx);
    }
}
