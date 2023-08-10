// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Models.Request;
using Microsoft.SemanticKernel.Orchestration;

namespace CopilotChat.WebApi.Utilities;

/// <summary>
/// Converts <see cref="Ask"/> variables to <see cref="ContextVariables"/>, inserting some system variables along the way.
/// </summary>
public class AskConverter
{
    private readonly IAuthInfo _authInfo;

    public AskConverter(IAuthInfo authInfo)
    {
        this._authInfo = authInfo;
    }

    /// <summary>
    /// Converts <see cref="Ask"/> variables to <see cref="ContextVariables"/>, inserting some system variables along the way.
    /// </summary>
    public ContextVariables GetContextVariables(Ask ask)
    {
        const string userIdKey = "userId";
        const string userNameKey = "userName";
        var contextVariables = new ContextVariables(ask.Input);
        foreach (var variable in ask.Variables)
        {
            if (variable.Key != userIdKey && variable.Key != userNameKey)
            {
                contextVariables.Set(variable.Key, variable.Value);
            }
        }

        contextVariables.Set(userIdKey, this._authInfo.UserId);
        contextVariables.Set(userNameKey, this._authInfo.Name);
        return contextVariables;
    }
}
