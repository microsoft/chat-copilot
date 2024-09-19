// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private readonly Kernel _kernel;

    public SemanticKernelProvider(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        QAzureOpenAIChatOptions qAzureOpenAIChatOptions
    )
    {
        this._kernel = InitializeCompletionKernel(
            serviceProvider,
            configuration,
            httpClientFactory,
            qAzureOpenAIChatOptions
        );
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public Kernel GetCompletionKernel() => this._kernel.Clone();

    private static Kernel InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        QAzureOpenAIChatOptions qAzureOpenAIChatOptions
    )
    {
        var builder = Kernel.CreateBuilder();

        builder.Services.AddLogging();

        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var defaultModel = qAzureOpenAIChatOptions.DefaultModel;
                foreach (
                    QAzureOpenAIChatOptions.OpenAIDeploymentConnection connection in qAzureOpenAIChatOptions.OpenAIDeploymentConnections
                )
                {
                    foreach (var deployment in connection.ChatCompletionDeployments)
                    {
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                        builder.AddAzureOpenAIChatCompletion(
                            deployment,
                            connection.Endpoint?.ToString(),
                            connection.APIKey,
                            httpClient: httpClientFactory.CreateClient(),
                            serviceId: deployment == defaultModel ? "default" : deployment
                        );
                    }
                }
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                builder.AddOpenAIChatCompletion(
                    openAIOptions.TextModel,
                    openAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient()
                );
#pragma warning restore CA2000
                break;

            default:
                throw new ArgumentException(
                    $"Invalid {nameof(memoryOptions.TextGeneratorType)} value in 'KernelMemory' settings."
                );
        }

        return builder.Build();
    }
}
