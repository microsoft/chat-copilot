// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using Microsoft.AspNetCore.Http;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Defines a service that performs content safety analysis on images.
/// </summary>
public interface IContentSafetyService : IDisposable
{
    /// <summary>
    /// Invokes a sync API to perform harmful content analysis on image.
    /// </summary>
    /// <param name="formFile">Image content file</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the image analysis response.</returns>
    Task<ImageAnalysisResponse> ImageAnalysisAsync(IFormFile formFile, CancellationToken cancellationToken);

    /// <summary>
    /// Parse the analysis result and return the violated categories.
    /// </summary>
    /// <param name="imageAnalysisResponse">The content analysis result.</param>
    /// <param name="threshold">Optional violation threshold.</param>
    /// <returns>The list of violated category names. Will return an empty list if there is no violation.</returns>
    List<string> ParseViolatedCategories(ImageAnalysisResponse imageAnalysisResponse, short threshold);
}
