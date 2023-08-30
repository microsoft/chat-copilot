// Copyright (c) Microsoft. All rights reserved.

using System;
using CopilotChat.Core;
using Microsoft.AspNetCore.Builder;
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

// ********************************************************
// ************** START ***********************************
// ********************************************************

var app = builder.Build();

app.Logger.LogInformation(
    "Starting Chat Copilot Memory pipeline service, .NET Env: {0}, Log Level: {1}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName());

app.Run();
