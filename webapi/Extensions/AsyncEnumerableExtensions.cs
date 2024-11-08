﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for enabling async LINQ operations on IAsyncEnumerable sequence.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Creates a List<T> from an IAsyncEnumerable<T> by enumerating it asynchronously.
    /// </summary>
    internal static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source)
        {
            result.Add(item);
        }

        return result;
    }
}
