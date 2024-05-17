// Copyright (c) Microsoft. All rights reserved.

using System;
using CopilotChat.Shared.Ocr.Tesseract;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.DataFormats;

namespace CopilotChat.Shared.Ocr;

/// <summary>
/// Dependency injection for kernel memory using configuration defined in appsettings.json
/// </summary>
public static class ConfigurationExtensions
{
    private const string ConfigOcrType = "ImageOcrType";

    public static IOcrEngine? CreateCustomOcr(this IConfiguration configuration)
    {
        var ocrType = configuration.GetSection($"{MemoryConfiguration.KernelMemorySection}:{ConfigOcrType}").Value ?? string.Empty;
        switch (ocrType)
        {
            case string x when x.Equals(TesseractOptions.SectionName, StringComparison.OrdinalIgnoreCase):
                var tesseractOptions =
                    configuration
                        .GetSection($"{MemoryConfiguration.KernelMemorySection}:{MemoryConfiguration.ServicesSection}:{TesseractOptions.SectionName}")
                        .Get<TesseractOptions>();

                if (tesseractOptions == null)
                {
                    throw new ArgumentNullException($"Missing configuration for {ConfigOcrType}: {ocrType}");
                }

                return new TesseractOcrEngine(tesseractOptions);

            default: // Allow for fall-through for standard OCR settings
                break;
        }

        return null;
    }
}
