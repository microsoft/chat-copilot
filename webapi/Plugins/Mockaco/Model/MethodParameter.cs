using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Plugins.OpenApi.GitHubPlugin.Model;

public class MethodParameter
{
  public string Name { get; set; }
  public string DataType { get; set; }
  public bool Required { get; set; }
  public string Description { get; set; }

}