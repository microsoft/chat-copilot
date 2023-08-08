// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace CopilotChat.WebApi.Models.Request;

public class Ask
{
    public string Input { get; set; } = string.Empty;

    public IEnumerable<KeyValuePair<string, string>> Variables { get; set; } = Enumerable.Empty<KeyValuePair<string, string>>();
}
