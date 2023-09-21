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

    /// <summary>
    /// Construct provider based on if images are supported, or not.
    /// </summary>
    /// <param name="allowImageOcr">Flag indicating if image ocr is supported</param>
    public DocumentTypeProvider(bool allowImageOcr)
    {
        this.supportedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { FileExtensions.MarkDown, false },
                { FileExtensions.MsWord, false },
                { FileExtensions.MsWordX, false },
                { FileExtensions.Pdf, false },
                { FileExtensions.PlainText, false },
                { FileExtensions.ImageBmp, true },
                { FileExtensions.ImageGif, true },
                { FileExtensions.ImagePng, true },
                { FileExtensions.ImageJpg, true },
                { FileExtensions.ImageJpeg, true },
                { FileExtensions.ImageTiff, true },
            };
    }

    /// <summary>
    /// Returns true if the extension is supported for import.
    /// </summary>
    /// <param name="extension">The file extension</param>
    /// <param name="isSafetyTarget">Is the document a target for content safety, if enabled?</param>
    /// <returns></returns>
    public bool IsSupported(string extension, out bool isSafetyTarget)
    {
        return this.supportedTypes.TryGetValue(extension, out isSafetyTarget);
    }
}
