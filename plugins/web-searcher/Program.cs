// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Plugins.WebSearcher.Models;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(configuration =>
    {
        // `ConfigureFunctionsWorkerDefaults` already adds environment variables as a source.
        configuration
            .AddUserSecrets<Program>(optional: true)
            .AddJsonFile(path: "local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices(services =>
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            // `ConfigureFunctionsWorkerDefaults` sets the default to ignore casing already.
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        var pluginConfig = services.BuildServiceProvider().GetService<IConfiguration>()?.GetSection(nameof(PluginConfig)).Get<PluginConfig>();
        services.AddSingleton<PluginConfig>(pluginConfig!);

        services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
        {
            var options = new OpenApiConfigurationOptions()
            {
                Info = new OpenApiInfo()
                {
                    Version = "1.0.0",
                    Title = "Web Searcher Plugin",
                    Description = "This plugin is capable of searching the internet."
                },
                Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
                OpenApiVersion = OpenApiVersionType.V3,
                IncludeRequestingHostName = true,
                ForceHttps = false,
                ForceHttp = false,
            };

            return options;
        });
    })
    .Build();

host.Run();
