// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CopilotChat.WebApi.Middleware;

/// <summary>
/// Middleware that replaces placeholders with their corresponding app setting values.
///
/// Ideally, this middleware should not exist and the frontend should dynamically get
/// its configuration from a controller rather than having this middleware provide it.
///
/// However, extensive React code changes are needed before this can be done.
/// </summary>
public class ResponseTokenReplacerMiddleware
{
    private const string OpeningTag = "<-=TOKEN=->";
    private const string ClosingTag = "</-=TOKEN=->";

    // Regex to find strings with letters, numbers, '-', '_', or ':' between token tags
    private readonly Regex _tokenRegex = new(@$"{OpeningTag}([\w\d-_:]+?){ClosingTag}", RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="next">Reference to next middleware to invoke.</param>
    public ResponseTokenReplacerMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store the original body stream for restoring the response body back to its original stream
        Stream originalBodyStream = context.Response.Body;

        // Create new memory stream for reading the response; Response body streams are write-only, therefore memory stream is needed here to read
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        // Call the next middleware
        await this._next(context);

        // Go to beginning of stream before reading
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Read the body from the stream
        using var streamReader = new StreamReader(memoryStream);
        string responseBodyText = await streamReader.ReadToEndAsync();

        // Replace tokens in response
        responseBodyText = this.ReplaceTokens(context, responseBodyText);

        // Write updated response to new memory stream
        using var modifiedMemoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(modifiedMemoryStream);
        await streamWriter.WriteAsync(responseBodyText);
        await streamWriter.FlushAsync();
        modifiedMemoryStream.Seek(0, SeekOrigin.Begin);
        context.Response.ContentLength = modifiedMemoryStream.Length;

        // Copy updated response to definitive body stream
        await modifiedMemoryStream.CopyToAsync(originalBodyStream);

        // Swap the original stream back in
        context.Response.Body = originalBodyStream;
    }

    private string ReplaceTokens(HttpContext context, string text)
    {
        IConfiguration appSettings = context.RequestServices.GetRequiredService<IConfiguration>();

        MatchCollection matches = this._tokenRegex.Matches(text);

        foreach (string token in matches.Select(m => m.Groups[1].Value).Distinct())
        {
            string? settingValue = appSettings.GetValue<string>(token);

            text = text.Replace($"{OpeningTag}{token}{ClosingTag}", settingValue, StringComparison.InvariantCulture);
        }

        return text;
    }
}

public static class StaticFileModifierMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseTokenReplacer(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseTokenReplacerMiddleware>();
    }
}
