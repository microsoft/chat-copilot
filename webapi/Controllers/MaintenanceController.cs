// Copyright (c) Microsoft. All rights reserved.

using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services.MemoryMigration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller for reporting the status of chat migration.
/// </summary>
[ApiController]
public class MaintenanceController : ControllerBase
{
    internal const string GlobalSiteMaintenance = "GlobalSiteMaintenance";

    private readonly ILogger<MaintenanceController> _logger;
    private readonly IOptions<ServiceOptions> _serviceOptions;
    private readonly IAuthInfo _authInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaintenanceController"/> class.
    /// </summary>
    public MaintenanceController(
        ILogger<MaintenanceController> logger,
        IOptions<ServiceOptions> serviceOptions,
        IAuthInfo authInfo)
    {
        this._logger = logger;
        this._serviceOptions = serviceOptions;
        this._authInfo = authInfo;
    }

    /// <summary>
    /// Route for reporting the status of site maintenance.
    /// </summary>
    [Route("maintenancestatus/")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MaintenanceResult?>> GetMaintenanceStatusAsync(
        [FromServices] IChatMigrationMonitor migrationMonitor,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        CancellationToken cancellationToken = default)
    {
        MaintenanceResult? result = null;

        var migrationStatus = await migrationMonitor.GetCurrentStatusAsync(cancellationToken);

        if (migrationStatus != ChatMigrationStatus.None)
        {
            result =
                new MaintenanceResult
                {
                    Title = "Migrating Chat Memory",
                    Message = "An upgrade requires that all non-document memories be migrated.  This may take several minutes...",
                    Note = "Note: All document memories will need to be re-imported.",
                };
        }

        if (this._serviceOptions.Value.InMaintenance)
        {
            result = new MaintenanceResult(); // Default maintenance message
        }

        if (result != null)
        {
            return this.Ok(result);
        }

        return this.Ok();
    }
}
