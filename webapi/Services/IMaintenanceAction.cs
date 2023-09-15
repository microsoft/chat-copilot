// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using System.Threading;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Defines discrete maintenance action responsible for both inspecting state
/// and performing maintenace.
/// </summary>
public interface IMaintenanceAction
{
    /// <summary>
    /// Calling site to initiate maintenance action.
    /// </summary>
    /// <returns>true if maintenance needed or in progress</returns>
    Task<bool> InvokeAsync(CancellationToken cancellation = default);
}
