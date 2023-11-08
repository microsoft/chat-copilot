// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Interface for common telemetry events to track actions across the semantic kernel.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Creates a telemetry event when a function is executed.
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <param name="functionName">Function name</param>
    /// <param name="success">If the function executed successfully</param>
    void TrackPluginFunction(string pluginName, string functionName, bool success);
}
