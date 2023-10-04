// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Models.Response;

namespace CopilotChat.WebApi.Models.Request;

public class ExecutePlanParameters : Ask
{
    public ProposedPlan? Plan { get; set; }
}
