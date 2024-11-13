// Copyright (c) Microsoft. All rights reserved.

using Microsoft.KernelMemory.DataFormats;
using Tesseract;

namespace CopilotChat.Shared.Ocr.Tesseract;

/// <summary>
/// Wrapper for the TesseractEngine within the Tesseract OCR library.
/// </summary>
public sealed class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly TesseractEngine _engine;

    /// <summary>
    /// Creates a new instance of the TesseractEngineWrapper passing in a valid TesseractEngine.
    /// </summary>
    public TesseractOcrEngine(TesseractConfig tesseractConfig)
    {
        if (!Directory.Exists(tesseractConfig.FilePath))
        {
            throw new DirectoryNotFoundException($"{tesseractConfig.FilePath} dir not found");
        }

        this._engine = new TesseractEngine(tesseractConfig.FilePath, tesseractConfig.Language);
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        await using (var imgStream = new MemoryStream())
        {
            await imageContent.CopyToAsync(imgStream, cancellationToken);
            imgStream.Position = 0;

            using var img = Pix.LoadFromMemory(imgStream.ToArray());

            using var page = this._engine.Process(img);
            return page.GetText();
        }
    }

    ///<inheritdoc/>
    public void Dispose()
    {
        this._engine.Dispose();
    }
}
