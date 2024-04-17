// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

using System.Net.Http;
using System.Threading.Tasks;
using CopilotChat.WebApi.Plugins.Utils;
using System.Net.Http.Json;
using System.Collections.Generic;
using CopilotChat.WebApi.Plugins.OpenApi.GitHubPlugin.Model;
using System.Security.Policy;
using Microsoft.AspNetCore.Routing;
using JetBrains.Annotations;

namespace Plugins;

public sealed class MockacoPlugin
{
  [KernelFunction, Description("What groups or categories of APIs do we support")]
  public async static Task<List<AppCatGroup>> GetAppCatalogGroups()
  {
    using var response = await new HttpClient().GetAsync(new Uri($"{ConstantUtils.ApiBaseUrl}__getappcataloggroups"));
    Console.WriteLine($"GetAppCatalogGroups response {response.StatusCode}");

    var appCatGroups = await response.Content.ReadFromJsonAsync<List<AppCatGroup>>();

    Console.WriteLine($"appCatGroups: {appCatGroups}");

    return appCatGroups;
  }


  [KernelFunction, Description("What is the Group Name of an App Catalog Group")]
  public async Task<string> GetAppCatalogGroupName(
    [Description("The Group Code")] string groupCode)
  {
    try
    {
      using var response = await new HttpClient().GetAsync($"{ConstantUtils.ApiBaseUrl}__getappcataloggroupname?groupCode={groupCode}");
      var groupName = await response.Content.ReadAsStringAsync();

      Console.WriteLine($"GET APP CATALOGUE GROUPNAME: {groupName}");

      return groupName;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Exception in GetAppCatalogGroups: {ex.Message}");
    }

    return null;
  }

  [KernelFunction, Description("What App Catalog Methods are supported for a given Group Code")]
  public async Task<List<AppCatalogMethod>> GetAppCatalogMethods(
    [Description("The Group Code")] string groupCode)
  {
    try
    {
      Console.WriteLine("GetAppCatalogMethods");

      using var response = await new HttpClient().GetAsync(new Uri($"{ConstantUtils.ApiBaseUrl}__getappcatalogmethods?groupCode={groupCode}"));
      Console.WriteLine($"GetAppCatalogMethods response {response.StatusCode}");
      var appCatalogMethods = await response.Content.ReadFromJsonAsync<List<AppCatalogMethod>>();

      Console.WriteLine($"appCatGroups: {appCatalogMethods}");

      return appCatalogMethods;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Exception in GetAppCatalogMethods: {ex.Message}");
    }

    return null;
  }

  [KernelFunction, Description("Get details about an App Catalog Method")]
  public async Task<AppCatalogMethod> GetAppCatalogMethod(
    [Description("The Group Code")] string groupCode,
    [Description("url path for the api call")] string id)
  {
    try
    {
      Console.WriteLine($"GetAppCatalogMethod groupCode:{groupCode} Id: {id}");

      using var response = await new HttpClient().GetAsync($"{ConstantUtils.ApiBaseUrl}__getappcatalogmethod?groupCode={groupCode}&id={id}");
      Console.WriteLine($"GetAppCatalogMethod response {response.StatusCode}");
      var appCatalogMethod = await response.Content.ReadFromJsonAsync<AppCatalogMethod>();

      Console.WriteLine($"{appCatalogMethod.GroupCode}, {appCatalogMethod.DomainName}, {appCatalogMethod.MethodType}, {appCatalogMethod.MethodPrefix}, {appCatalogMethod.Method}, {appCatalogMethod.MethodDescription}, {appCatalogMethod.FileName}");

      return appCatalogMethod;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Exception in GetAppCatalogMethod: {ex.Message}");
    }

    return null;
  }

  [KernelFunction, Description("Get details about parameters for a method/function")]
  public async Task<List<MethodParameter>> GetMethodParameters(
    [Description("The Group Code")] string groupCode,
    [Description("The Domain Name")] string domainName,
    [Description("The Method Type")] string methodType,
    [Description("The Method")] string method)
  {
    try
    {
      using var response = await new HttpClient().GetAsync(
          $"{ConstantUtils.ApiBaseUrl}__getmethodparameters?groupCode={groupCode}&domainName={domainName}&methodType={methodType}&method={method}");
      Console.WriteLine($"GetMethodParameters response {response.StatusCode}");
      var methodParameters = await response.Content.ReadFromJsonAsync<List<MethodParameter>>();

      return methodParameters;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Exception in GetMethodParameters: {ex.Message}");
    }

    return null;
  }

  [KernelFunction, Description("Example output for a method/function")]
  public async Task<string> GetMethodExample(
    [Description("The Group Code")] string groupCode,
    [Description("The Domain Name")] string domainName,
    [Description("The Method Type")] string methodType,
    [Description("The Method")] string method
  )
  {
    try
    {
      using var response = await new HttpClient().GetAsync(
        $"{ConstantUtils.ApiBaseUrl}__getrequestbodyexample?groupCode={groupCode}&domainName={domainName}&methodType={methodType}&method={method}");
      Console.WriteLine($"GetMethodExample response {response.StatusCode}");
      var exampleText = await response.Content.ReadAsStringAsync();

      return exampleText;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Exception in GetMethodExample: {ex.Message}");
    }

    return null;
  }
}