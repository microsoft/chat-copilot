// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CopilotChat.WebApi.Plugins.Utils;

/// <summary>
/// Utility methods for working with asynchronous operations and callbacks.
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Try to optimize json from the planner response
    /// based on token limit
    /// </summary>
    internal static string OptimizeOdataResponseJson(string jsonString, int tokenLimit)
    {
        // Remove all new line characters + leading and trailing white space
        jsonString = Regex.Replace(jsonString.Trim(), @"[\n\r]", string.Empty);
        var document = JsonDocument.Parse(jsonString);

        // The json will be deserialized based on the response type of the particular operation that was last invoked by the planner
        // The response type can be a custom trimmed down json structure, which is useful in staying within the token limit
        Type responseType = typeof(object);

        // Deserializing limits the json content to only the fields defined in the respective OpenApi's Model classes
        var functionResponse = JsonSerializer.Deserialize(jsonString, responseType);
        jsonString = functionResponse != null ? JsonSerializer.Serialize(functionResponse) : string.Empty;
        document = JsonDocument.Parse(jsonString);

        int jsonStringTokenCount = TokenUtils.TokenCount(jsonString);

        // Return the JSON content if it does not exceed the token limit
        if (jsonStringTokenCount < tokenLimit)
        {
            return jsonString;
        }

        List<object> itemList = new();

        // Some APIs will return a JSON response with one property key representing an embedded answer.
        // Extract this value for further processing
        string resultsDescriptor = string.Empty;

        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            if (document.RootElement.TryGetProperty("value", out JsonElement valueElement))
            {
                if (document.RootElement.TryGetProperty("@odata.context", out JsonElement odataContext))
                {
                    // Save property name for result interpolation
                    var odataContextVal = odataContext.GetRawText();
                    tokenLimit -= TokenUtils.TokenCount(odataContextVal);
                    resultsDescriptor = string.Format(CultureInfo.InvariantCulture, "{0}: ", odataContextVal);
                }

                // Extract object to be truncated  
                var valueDocument = JsonDocument.Parse(valueElement.GetRawText());
                document = valueDocument;
            }
        }

        // Detail Object
        // To stay within token limits, attempt to truncate the list of properties
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                int propertyTokenCount = TokenUtils.TokenCount(property.ToString());

                if (tokenLimit - propertyTokenCount > 0)
                {
                    itemList.Add(property);
                    tokenLimit -= propertyTokenCount;
                }
                else
                {
                    break;
                }
            }
        }

        // Summary (List) Object
        // To stay within token limits, attempt to truncate the list of results
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                int itemTokenCount = TokenUtils.TokenCount(item.ToString());

                if (tokenLimit - itemTokenCount > 0)
                {
                    itemList.Add(item);
                    tokenLimit -= itemTokenCount;
                }
                else
                {
                    break;
                }
            }
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}{1}", resultsDescriptor, JsonSerializer.Serialize(itemList));
    }
}
