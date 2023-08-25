// Copyright (c) Microsoft. All rights reserved.

// ********************************************************
// ************** APP BUILD *******************************
// ********************************************************

using System;
using CopilotChat.MemoryPipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Diagnostics;

var app = WebApplication.CreateBuilder().AddMemoryServices().Build();

// ********************************************************
// ************** START ***********************************
// ********************************************************

app.Logger.LogInformation(
    "Starting Chat Copilot Memory pipeline service, .NET Env: {0}, Log Level: {1}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName());

app.Run();
