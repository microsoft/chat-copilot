// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// An OCR engine that can read in text from image MIME type files.
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Reads all text from the image file.
    /// </summary>
    /// <param name="imageFile">A file that is expected to be an image MIME type</param>
    /// <returns></returns>
    Task<string> ReadTextFromImageFileAsync(IFormFile imageFile);
}
