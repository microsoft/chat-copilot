// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Response;

namespace CopilotChat.WebApi.Models.Request;

public class ExecutePlanParameters : Ask
{
    public ProposedPlan? Plan { get; set; }

    /// <summary>
    /// Id of the message containing proposed plan previously saved in chat history, if any.
    /// </summary>
    public string? MessageId { get; set; }
}
