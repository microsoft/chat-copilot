// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace CopilotChat.WebApi.Models.Request;

public class Ask
{
    public virtual string Input { get; set; } = string.Empty;

    public IEnumerable<KeyValuePair<string, string>> Variables { get; set; } = Enumerable.Empty<KeyValuePair<string, string>>();
}

// TODO: Does it make sense to keep this here? Or should combine Ask and ChatAsk into one, under CopilotChat/Models?
