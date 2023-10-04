// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Services.MemoryMigration;
using CopilotChat.WebApi.Storage;
using CopilotChat.WebApi.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// Add options and services for Copilot Chat.
/// </summary>
public static class CopilotChatServiceExtensions
{
    private const string SemanticMemoryOptionsName = "SemanticMemory";

    /// <summary>
    /// Parse configuration into options.
    /// </summary>
    public static IServiceCollection AddOptions(this IServiceCollection services, ConfigurationManager configuration)
    {
        // General configuration
        AddOptions<ServiceOptions>(ServiceOptions.PropertyName);

        // Authentication configuration
        AddOptions<ChatAuthenticationOptions>(ChatAuthenticationOptions.PropertyName);

        // Chat storage configuration
        AddOptions<ChatStoreOptions>(ChatStoreOptions.PropertyName);

        // Azure speech token configuration
        AddOptions<AzureSpeechOptions>(AzureSpeechOptions.PropertyName);

        AddOptions<DocumentMemoryOptions>(DocumentMemoryOptions.PropertyName);

        // Chat prompt options
        AddOptions<PromptsOptions>(PromptsOptions.PropertyName);

        AddOptions<PlannerOptions>(PlannerOptions.PropertyName);

        AddOptions<ContentSafetyOptions>(ContentSafetyOptions.PropertyName);

        AddOptions<SemanticMemoryConfig>(SemanticMemoryOptionsName);

        AddOptions<FrontendOptions>(FrontendOptions.PropertyName);

        return services;

        void AddOptions<TOptions>(string propertyName)
            where TOptions : class
        {
            services.AddOptions<TOptions>(configuration.GetSection(propertyName));
        }
    }

    internal static void AddOptions<TOptions>(this IServiceCollection services, IConfigurationSection section)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart()
            .PostConfigure(TrimStringProperties);
    }

    internal static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        return services.AddScoped<AskConverter>();
    }

    internal static IServiceCollection AddPlugins(this IServiceCollection services, IConfiguration configuration)
    {
        var plugins = configuration.GetSection("Plugins").Get<List<Plugin>>() ?? new List<Plugin>();
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
        logger.LogDebug("Found {0} plugins.", plugins.Count);

        // Validate the plugins
        Dictionary<string, Plugin> validatedPlugins = new();
        foreach (Plugin plugin in plugins)
        {
            if (validatedPlugins.ContainsKey(plugin.Name))
            {
                logger.LogWarning("Plugin '{0}' is defined more than once. Skipping...", plugin.Name);
                continue;
            }

            var pluginManifestUrl = PluginUtils.GetPluginManifestUri(plugin.ManifestDomain);
            using var request = new HttpRequestMessage(HttpMethod.Get, pluginManifestUrl);
            // Need to set the user agent to avoid 403s from some sites.
            request.Headers.Add("User-Agent", Telemetry.HttpUserAgent);
            try
            {
                logger.LogInformation("Adding plugin: {0}.", plugin.Name);
                using var httpClient = new HttpClient();
                var response = httpClient.SendAsync(request).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Plugin '{plugin.Name}' at '{pluginManifestUrl}' returned status code '{response.StatusCode}'.");
                }
                validatedPlugins.Add(plugin.Name, plugin);
                logger.LogInformation("Added plugin: {0}.", plugin.Name);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is AggregateException)
            {
                logger.LogWarning(ex, "Plugin '{0}' at {1} responded with error. Skipping...", plugin.Name, pluginManifestUrl);
            }
            catch (Exception ex) when (ex is UriFormatException)
            {
                logger.LogWarning("Plugin '{0}' at {1} is not a valid URL. Skipping...", plugin.Name, pluginManifestUrl);
            }
        }

        // Add the plugins
        services.AddSingleton<IDictionary<string, Plugin>>(validatedPlugins);

        return services;
    }

    internal static IServiceCollection AddMaintenanceServices(this IServiceCollection services)
    {
        // Inject migration services
        services.AddSingleton<IChatMigrationMonitor, ChatMigrationMonitor>();
        services.AddSingleton<IChatMemoryMigrationService, ChatMemoryMigrationService>();

        // Inject actions so they can be part of the action-list.
        services.AddSingleton<ChatMigrationMaintenanceAction>();
        services.AddSingleton<IReadOnlyList<IMaintenanceAction>>(
            sp =>
                (IReadOnlyList<IMaintenanceAction>)
                new[]
                {
                    sp.GetRequiredService<ChatMigrationMaintenanceAction>(),
                });

        return services;
    }

    /// <summary>
    /// Add CORS settings.
    /// </summary>
    internal static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        string[] allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowedOrigins.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                            .WithMethods("POST", "GET", "PUT", "DELETE", "PATCH")
                            .AllowAnyHeader();
                    });
            });
        }

        return services;
    }

    /// <summary>
    /// Add persistent chat store services.
    /// </summary>
    public static IServiceCollection AddPersistentChatStore(this IServiceCollection services)
    {
        IStorageContext<ChatSession> chatSessionStorageContext;
        IStorageContext<ChatMessage> chatMessageStorageContext;
        IStorageContext<MemorySource> chatMemorySourceStorageContext;
        IStorageContext<ChatParticipant> chatParticipantStorageContext;

        ChatStoreOptions chatStoreConfig = services.BuildServiceProvider().GetRequiredService<IOptions<ChatStoreOptions>>().Value;

        switch (chatStoreConfig.Type)
        {
            case ChatStoreOptions.ChatStoreType.Volatile:
            {
                chatSessionStorageContext = new VolatileContext<ChatSession>();
                chatMessageStorageContext = new VolatileContext<ChatMessage>();
                chatMemorySourceStorageContext = new VolatileContext<MemorySource>();
                chatParticipantStorageContext = new VolatileContext<ChatParticipant>();
                break;
            }

            case ChatStoreOptions.ChatStoreType.Filesystem:
            {
                if (chatStoreConfig.Filesystem == null)
                {
                    throw new InvalidOperationException("ChatStore:Filesystem is required when ChatStore:Type is 'Filesystem'");
                }

                string fullPath = Path.GetFullPath(chatStoreConfig.Filesystem.FilePath);
                string directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
                chatSessionStorageContext = new FileSystemContext<ChatSession>(
                    new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_sessions{Path.GetExtension(fullPath)}")));
                chatMessageStorageContext = new FileSystemContext<ChatMessage>(
                    new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_messages{Path.GetExtension(fullPath)}")));
                chatMemorySourceStorageContext = new FileSystemContext<MemorySource>(
                    new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_memorysources{Path.GetExtension(fullPath)}")));
                chatParticipantStorageContext = new FileSystemContext<ChatParticipant>(
                    new FileInfo(Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(fullPath)}_participants{Path.GetExtension(fullPath)}")));
                break;
            }

            case ChatStoreOptions.ChatStoreType.Cosmos:
            {
                if (chatStoreConfig.Cosmos == null)
                {
                    throw new InvalidOperationException("ChatStore:Cosmos is required when ChatStore:Type is 'Cosmos'");
                }
#pragma warning disable CA2000 // Dispose objects before losing scope - objects are singletons for the duration of the process and disposed when the process exits.
                chatSessionStorageContext = new CosmosDbContext<ChatSession>(
                    chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatSessionsContainer);
                chatMessageStorageContext = new CosmosDbContext<ChatMessage>(
                    chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatMessagesContainer);
                chatMemorySourceStorageContext = new CosmosDbContext<MemorySource>(
                    chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatMemorySourcesContainer);
                chatParticipantStorageContext = new CosmosDbContext<ChatParticipant>(
                    chatStoreConfig.Cosmos.ConnectionString, chatStoreConfig.Cosmos.Database, chatStoreConfig.Cosmos.ChatParticipantsContainer);
#pragma warning restore CA2000 // Dispose objects before losing scope
                break;
            }

            default:
            {
                throw new InvalidOperationException(
                    "Invalid 'ChatStore' setting 'chatStoreConfig.Type'.");
            }
        }

        services.AddSingleton<ChatSessionRepository>(new ChatSessionRepository(chatSessionStorageContext));
        services.AddSingleton<ChatMessageRepository>(new ChatMessageRepository(chatMessageStorageContext));
        services.AddSingleton<ChatMemorySourceRepository>(new ChatMemorySourceRepository(chatMemorySourceStorageContext));
        services.AddSingleton<ChatParticipantRepository>(new ChatParticipantRepository(chatParticipantStorageContext));

        return services;
    }

    /// <summary>
    /// Add authorization services
    /// </summary>
    public static IServiceCollection AddCopilotChatAuthorization(this IServiceCollection services)
    {
        return services.AddScoped<IAuthorizationHandler, ChatParticipantAuthorizationHandler>()
            .AddAuthorizationCore(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy(AuthPolicyName.RequireChatParticipant, builder =>
                {
                    builder.RequireAuthenticatedUser()
                        .AddRequirements(new ChatParticipantRequirement());
                });
            });
    }

    /// <summary>
    /// Add authentication services
    /// </summary>
    public static IServiceCollection AddCopilotChatAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthInfo, AuthInfo>();
        var config = services.BuildServiceProvider().GetRequiredService<IOptions<ChatAuthenticationOptions>>().Value;
        switch (config.Type)
        {
            case ChatAuthenticationOptions.AuthenticationType.AzureAd:
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(configuration.GetSection($"{ChatAuthenticationOptions.PropertyName}:AzureAd"));
                break;

            case ChatAuthenticationOptions.AuthenticationType.None:
                services.AddAuthentication(PassThroughAuthenticationHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, PassThroughAuthenticationHandler>(
                        authenticationScheme: PassThroughAuthenticationHandler.AuthenticationScheme,
                        configureOptions: null);
                break;

            default:
                throw new InvalidOperationException($"Invalid authentication type '{config.Type}'.");
        }

        return services;
    }

    /// <summary>
    /// Trim all string properties, recursively.
    /// </summary>
    private static void TrimStringProperties<T>(T options) where T : class
    {
        Queue<object> targets = new();
        targets.Enqueue(options);

        while (targets.Count > 0)
        {
            object target = targets.Dequeue();
            Type targetType = target.GetType();
            foreach (PropertyInfo property in targetType.GetProperties())
            {
                // Skip enumerations
                if (property.PropertyType.IsEnum)
                {
                    continue;
                }

                // Skip index properties
                if (property.GetIndexParameters().Length == 0)
                {
                    continue;
                }

                // Property is a built-in type, readable, and writable.
                if (property.PropertyType.Namespace == "System" &&
                    property.CanRead &&
                    property.CanWrite)
                {
                    // Property is a non-null string.
                    if (property.PropertyType == typeof(string) &&
                        property.GetValue(target) != null)
                    {
                        property.SetValue(target, property.GetValue(target)!.ToString()!.Trim());
                    }
                }
                else
                {
                    // Property is a non-built-in and non-enum type - queue it for processing.
                    if (property.GetValue(target) != null)
                    {
                        targets.Enqueue(property.GetValue(target)!);
                    }
                }
            }
        }
    }
}
