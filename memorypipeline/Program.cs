// Copyright (c) Microsoft. All rights reserved.

// ********************************************************
// ************** APP BUILD *******************************
// ********************************************************

using System;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticMemory.Core.Configuration;
using Microsoft.SemanticMemory.Core.Diagnostics;
using CopilotChat.MemoryPipeline;

var app = Builder.CreateBuilder(out SemanticMemoryConfig config).Build();

// ********************************************************
// ************** START ***********************************
// ********************************************************

app.Logger.LogInformation(
    "Starting Copilot Chat Memory pipeline service, .NET Env: {0}, Log Level: {1}",
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    app.Logger.GetLogLevelName());

app.Run();
