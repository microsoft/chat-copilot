// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.SemanticMemory.Pipeline;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Defines a service that performs content safety analysis on images.
/// </summary>
public class DocumentTypeProvider
{
    private readonly Dictionary<string, bool> supportedTypes;

    public DocumentTypeProvider(bool allowImageOcr)
    {
        this.supportedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { FileExtensions.MarkDown, false },
                { FileExtensions.MsWord, false }, // $$$ CONTENT SAFETY ???
                { FileExtensions.MsWordX, false }, // $$$ CONTENT SAFETY ???
                { FileExtensions.Pdf, false }, // $$$ CONTENT SAFETY ???
                { FileExtensions.PlainText, false },
                //{ FileExtensions.Bmp, true },
                //{ FileExtensions.Gif, true },
                //{ FileExtensions.Png, true },
                //{ FileExtensions.Jpg, true },
                //{ FileExtensions.Jpeg, true },
                //{ FileExtensions.Tiff, true },
            };
    }

    public bool IsSupported(string extension, out bool isSafetyTarget)
    {
        return this.supportedTypes.TryGetValue(extension, out isSafetyTarget);
    }
}
