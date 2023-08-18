// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Defines a service that performs content safety analysis on images.
/// </summary>
public interface IContentSafetyService : IDisposable
{
    /// <summary>
    /// Gets the options for the content safety.
    /// </summary>
    ContentSafetyOptions Options { get; }

    /// <summary>
    /// Checks the state of the content safety.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <returns>True if content safety is enabled with non-null endpoint.</returns>
    bool ContentSafetyStatus(ILogger logger);

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
    /// <param name="threshold">Optional violation threshold. If not specified, threshold should be pulled from Options.</param>
    /// <returns>The list of violated category names. Will return an empty list if there is no violation.</returns>
    List<string> ParseViolatedCategories(ImageAnalysisResponse imageAnalysisResponse, short? threshold);
}
