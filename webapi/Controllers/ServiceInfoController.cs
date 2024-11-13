﻿// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Reflection;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for returning information on the service.
/// </summary>
[ApiController]
public class ServiceInfoController : ControllerBase
{
    private readonly ILogger<ServiceInfoController> _logger;
    private readonly IConfiguration _configuration;
    private readonly KernelMemoryConfig _memoryOptions;
    private readonly ChatAuthenticationOptions _chatAuthenticationOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly IEnumerable<Plugin> _availablePlugins;
    private readonly ContentSafetyOptions _contentSafetyOptions;

    public ServiceInfoController(
        ILogger<ServiceInfoController> logger,
        IConfiguration configuration,
        IOptions<KernelMemoryConfig> memoryOptions,
        IOptions<ChatAuthenticationOptions> chatAuthenticationOptions,
        IOptions<FrontendOptions> frontendOptions,
        IDictionary<string, Plugin> availablePlugins,
        IOptions<ContentSafetyOptions> contentSafetyOptions)
    {
        this._logger = logger;
        this._configuration = configuration;
        this._memoryOptions = memoryOptions.Value;
        this._chatAuthenticationOptions = chatAuthenticationOptions.Value;
        this._frontendOptions = frontendOptions.Value;
        this._availablePlugins = this.SanitizePlugins(availablePlugins);
        this._contentSafetyOptions = contentSafetyOptions.Value;
    }

    /// <summary>
    /// Return information on running service.
    /// </summary>
    [Route("info")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetServiceInfo()
    {
        var response = new ServiceInfoResponse()
        {
            MemoryStore = new MemoryStoreInfoResponse()
            {
                Types = Enum.GetNames(typeof(MemoryStoreType)),
                SelectedType = this._memoryOptions.GetMemoryStoreType(this._configuration).ToString(),
            },
            AvailablePlugins = this._availablePlugins,
            Version = GetAssemblyFileVersion(),
            IsContentSafetyEnabled = this._contentSafetyOptions.Enabled
        };

        return this.Ok(response);
    }

    /// <summary>
    /// Return the auth config to be used by the frontend client to access this service.
    /// </summary>
    [Route("authConfig")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public IActionResult GetAuthConfig()
    {
        string authorityUriString = string.Empty;
        if (!string.IsNullOrEmpty(this._chatAuthenticationOptions.AzureAd!.Instance) &&
            !string.IsNullOrEmpty(this._chatAuthenticationOptions.AzureAd!.TenantId))
        {
            var authorityUri = new Uri(this._chatAuthenticationOptions.AzureAd!.Instance);
            authorityUri = new Uri(authorityUri, this._chatAuthenticationOptions.AzureAd!.TenantId);
            authorityUriString = authorityUri.ToString();
        }

        var config = new FrontendAuthConfig
        {
            AuthType = this._chatAuthenticationOptions.Type.ToString(),
            AadAuthority = authorityUriString,
            AadClientId = this._frontendOptions.AadClientId,
            AadApiScope = $"api://{this._chatAuthenticationOptions.AzureAd!.ClientId}/{this._chatAuthenticationOptions.AzureAd!.Scopes}",
        };

        return this.Ok(config);
    }

    private static string GetAssemblyFileVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

        return fileVersion.FileVersion ?? string.Empty;
    }

    /// <summary>
    /// Sanitize the plugins to only return the name and url.
    /// </summary>
    /// <param name="plugins">The plugins to sanitize.</param>
    /// <returns></returns>
    private IEnumerable<Plugin> SanitizePlugins(IDictionary<string, Plugin> plugins)
    {
        return plugins.Select(p => new Plugin()
        {
            Name = p.Value.Name,
            ManifestDomain = p.Value.ManifestDomain,
        });
    }
}
