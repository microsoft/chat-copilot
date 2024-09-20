using System.Collections.Generic;
using System.Linq;

namespace CopilotChat.WebApi.Extensions;

public static class QueryExtensions
{
    /// <summary>
    /// Returns all items if count is -1, otherwise returns the specified count of items.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <param name="source">The sequence to return elements from.</param>
    /// <param name="count">The number of elements to return or -1 for all elements.</param>
    /// <returns>An IEnumerable<T> that contains the requested number of elements from the start of the sequence.</returns>
    public static IEnumerable<T> TakeOrAll<T>(this IEnumerable<T> source, int count)
    {
        return count == -1 ? source : source.Take(count);
    }
}
