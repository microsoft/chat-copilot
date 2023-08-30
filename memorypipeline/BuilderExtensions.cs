// Copyright (c) Microsoft. All rights reserved.

using System;
using CopilotChat.MemoryPipeline.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticMemory;
using Microsoft.SemanticMemory.Configuration;

namespace CopilotChat.MemoryPipeline;

/// <summary>
/// Dependency injection for semantic-memory using configuration defined in appsettings.json
/// </summary>
internal static class BuilderExtensions
{
    private const string ConfigRoot = "SemanticMemory";
    private const string ConfigServices = "Services";
    private const string ConfigOcrType = "ImageOcrType";

    public static WebApplicationBuilder AddMemoryServices(this WebApplicationBuilder builder)
    {
        var memoryBuilder = new MemoryClientBuilder(builder.Services).FromAppSettings();

        InjectCustomOcr();

        ISemanticMemoryClient memory = memoryBuilder.Build();

        builder.Services.AddSingleton(memory);

        return builder;

        void InjectCustomOcr()
        {
            var ocrType = builder.Configuration.GetSection($"{ConfigRoot}:{ConfigOcrType}").Value;
            if (ocrType?.Equals(TesseractOptions.SectionName, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                var tesseractOptions =
                    builder.Configuration
                        .GetSection($"{ConfigRoot}:{ConfigServices}:{TesseractOptions.SectionName}")
                        .Get<TesseractOptions>();

                if (tesseractOptions == null)
                {
                    throw new ConfigurationException($"Missing configuration for {ConfigOcrType}: {ocrType}");
                }

                memoryBuilder.WithCustomImageOcr(new TesseractOcrEngine(tesseractOptions));
            }
        }
    }
}
