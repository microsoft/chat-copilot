// Copyright (c) Microsoft. All rights reserved.

using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CopilotChat.WebApi;

public interface IKernelFactory
{
    IKernel CreateNewKernel();
}

public class KernelFactory : IKernelFactory
{
    public ILogger Logger { get; private set; }

    public IOptions<AIServiceOptions> Options { get; private set; }

    public KernelFactory(ILogger logger, IOptions<AIServiceOptions> options)
    {
        this.Logger = logger;
        this.Options = options;
    }

    public IKernel CreateNewKernel()
    {
        AIServiceOptions options = this.Options.Value;

        IKernel kernel = Kernel.Builder
            .WithLogger(this.Logger)
            .WithAzureChatCompletionService(options.Models.Completion, options.Endpoint, options.Key)
            .WithAzureTextEmbeddingGenerationService(options.Models.Embedding, options.Endpoint, options.Key)
            .Build();

        return kernel;
    }
}
