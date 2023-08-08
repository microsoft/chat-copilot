// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel.DataAnnotations;
using CopilotChat.WebApi.Options;

namespace CopilotChat.WebApi.Models.Request;

public class ChatAsk : Ask
{
    [Required, NotEmptyOrWhitespace]
    public override string Input { get => base.Input; set => base.Input = value; }
}
