// Copyright (c) Microsoft. All rights reserved.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.KernelMemory.DataFormats;
using Tesseract;

namespace CopilotChat.Shared.Ocr.Tesseract;

/// <summary>
/// Wrapper for the TesseractEngine within the Tesseract OCR library.
/// </summary>
public class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly TesseractEngine _engine;

    /// <summary>
    /// Creates a new instance of the TesseractOcrEngine passing in a valid TesseractEngine.
    /// </summary>
    public TesseractOcrEngine(TesseractOptions tesseractOptions)
    {
        // Initialize TesseractEngine with provided options
        this._engine = new TesseractEngine(tesseractOptions.FilePath, tesseractOptions.Language);
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a buffer for CopyToAsync to reduce memory usage for large images
            await using (var imgStream = new MemoryStream())
            {
                await imageContent.CopyToAsync(imgStream, 81920, cancellationToken);  // Buffered copy with 80 KB buffer size
                imgStream.Position = 0; // Reset position for reading

                // Load image from memory and process with Tesseract
                using var img = Pix.LoadFromMemory(imgStream.ToArray());
                using var page = this._engine.Process(img);

                return page.GetText(); // Return the extracted text
            }
        }
        catch (OperationCanceledException)
        {
            // If operation is canceled, return an empty string or handle accordingly
            return string.Empty;
        }
    }

    /// <summary>
    /// Dispose the TesseractEngine resources.
    /// </summary>
    public void Dispose()
    {
        // Dispose of the TesseractEngine to free up resources
        _engine.Dispose();
    }
}
