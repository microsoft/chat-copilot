// Copyright (c) Microsoft. All rights reserved.
using CopilotChat.Shared.Ocr;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;

namespace CopilotChat.Shared;

/// <summary>
/// Dependency injection for semantic-memory using custom OCR configuration defined in appsettings.json
/// </summary>
public static class MemoryClientBuilderExtensions
{
    public static KernelMemoryBuilder WithCustomOcr(this KernelMemoryBuilder builder, IConfiguration configuration)
    {
        var ocrEngine = configuration.CreateCustomOcr();

        if (ocrEngine != null)
        {
            builder.WithCustomImageOcr(ocrEngine);
        }

        return builder;
    }
}
