using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Plugins.OpenApi.GitHubPlugin.Model;

public class AppCatGroup
{
  public AppCatGroup()
  {
    GroupCode = "";
    GroupName = "";
    GroupDescription = "";
    ShortCode = "";
    OverlayText = "";
    ApiKeys = new List<Application>();
  }

  public string GroupCode { get; set; }
  public string GroupName { get; set; }
  public string GroupDescription { get; set; }
  public string ShortCode { get; set; }
  public string OverlayText { get; set; }
  public List<Application> ApiKeys { get; set; }
}