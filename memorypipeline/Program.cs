// Copyright (c) Microsoft. All rights reserved.

using System;
using CopilotChat.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.Diagnostics;

// ********************************************************
// ************** SETUP ***********************************
// ********************************************************

var builder = WebApplication.CreateBuilder();

ISemanticMemoryClient memory =
    new MemoryClientBuilder(builder.Services)
        .FromAppSettings()
        .WithCustomOcr(builder.Configuration)
        .Build();

builder.Services.AddSingleton(memory);

builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

DateTimeOffset start = DateTimeOffset.UtcNow;

// Simple ping endpoint
app.MapGet("/", () =>
{
    var uptime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - start.ToUnixTimeSeconds();
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var message = $"Memory pipeline is running. Uptime: {uptime} secs.";
    if (!string.IsNullOrEmpty(environment))
    {
        message += $" Environment: {environment}";
    }
    return Results.Ok(message);
});

// ********************************************************
// ************** START ***********************************
// ********************************************************

app.Logger.LogInformation(
    "Starting Chat Copilot Memory pipeline service, .NET Env: {0}, Log Level: {1}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName());

app.Run();
