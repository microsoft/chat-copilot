// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticMemory.DataFormats.Image;
using Tesseract;

namespace CopilotChat.Shared.Ocr.Tesseract;

/// <summary>
/// Wrapper for the TesseractEngine within the Tesseract OCR library.
/// </summary>
public class TesseractOcrEngine : IOcrEngine
{
    private readonly TesseractEngine _engine;

    /// <summary>
    /// Creates a new instance of the TesseractEngineWrapper passing in a valid TesseractEngine.
    /// </summary>
    public TesseractOcrEngine(TesseractOptions tesseractOptions)
    {
        this._engine = new TesseractEngine(tesseractOptions.FilePath, tesseractOptions.Language);
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        await using (var imgStream = new MemoryStream())
        {
            await imageContent.CopyToAsync(imgStream);
            imgStream.Position = 0;

            using var img = Pix.LoadFromMemory(imgStream.ToArray());

            using var page = this._engine.Process(img);
            return page.GetText();
        }
    }
}
