// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Models.Request;
using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi.Utilities;

/// <summary>
/// Converts <see cref="Ask"/> variables to <see cref="KernelArguments"/>, inserting some system variables along the way.
/// </summary>
public class AskConverter
{
    private readonly IAuthInfo _authInfo;

    public AskConverter(IAuthInfo authInfo)
    {
        this._authInfo = authInfo;
    }

    /// <summary>
    /// Converts <see cref="Ask"/> variables to <see cref="KernelArguments"/>, inserting some system variables along the way.
    /// </summary>
    public KernelArguments GetKernelArguments(Ask ask)
    {
        const string userIdKey = "userId";
        const string userNameKey = "userName";
        var kernelArguments = new KernelArguments(ask.Input);
        foreach (var variable in ask.Variables)
        {
            if (variable.Key != userIdKey && variable.Key != userNameKey)
            {
                kernelArguments.Add(variable.Key, variable.Value);
            }
        }

        kernelArguments.Add(userIdKey, this._authInfo.UserId);
        kernelArguments.Add(userNameKey, this._authInfo.Name);
        return kernelArguments;
    }
}
