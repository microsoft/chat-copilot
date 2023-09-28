// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Diagnostics;

namespace CopilotChat.WebApi.Skills.Utils;

/// <summary>
/// Utility methods for working with asynchronous operations and callbacks.
/// </summary>
public static class AsyncUtils
{
    /// <summary>
    /// Invokes an asynchronous callback function and tags any exception that occurs with function name.
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the callback function.</typeparam>
    /// <param name="callback">The asynchronous callback function to invoke.</param>
    /// <param name="functionName">The name of the function that calls this method, for logging purposes.</param>
    /// <returns>A task that represents the asynchronous operation and contains the result of the callback function.</returns>
    public static async Task<T> SafeInvokeAsync<T>(Func<Task<T>> callback, string functionName)
    {
        try
        {
            // Invoke the callback and await the result
            return await callback();
        }
        catch (Exception ex)
        {
            throw new SKException($"{functionName} failed.", ex);
        }
    }

    /// <summary>
    /// Invokes an asynchronous callback function and tags any exception that occurs with function name.
    /// Same as SafeInvokeAsync<T> without the return type.
    /// </summary>
    /// <param name="callback">The asynchronous callback function to invoke.</param>
    /// <param name="functionName">The name of the function that calls this method, for logging purposes.</param>
    /// <returns>A task that represents the asynchronous operation and contains the result of the callback function.</returns>
    public static async Task SafeInvokeAsync(Func<Task> callback, string functionName)
    {
        try
        {
            // Invoke the callback and await the result
            await callback();
        }
        catch (Exception ex)
        {
            throw new SKException($"{functionName} failed.", ex);
        }
    }
}
