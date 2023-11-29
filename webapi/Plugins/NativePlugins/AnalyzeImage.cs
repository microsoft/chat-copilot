// Copyright (c) Microsoft. All rights reserved.
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.Drawing;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace CopilotChat.WebApi.Plugins.NativePlugins;

public class AnalyzeImage
{
    static readonly HttpClient client = new HttpClient();

    [SKFunction, Description("Analyze a given Image for spoilage")]
    public static async Task<string> DescribeQualityIssueAsync(
        [Description("Name of the image file to be analyzed")] string fileName,
        [Description("Quality issues to check for")] string qualityIssues)
    {
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_SECRET_KEY");
        string base64Image = EncodeImageToBase64("C:\\temp\\" + fileName);
        Console.WriteLine("C:\\temp\\" + fileName);

        string instructions = "This is an image of a product Check the image of the product for damage or spoilage " +
            "related to the following issues:" + qualityIssues + ". Also describe any other quality " +
            "issues that may be visible in the image";

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            model = "gpt-4-vision-preview",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = instructions },
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                    }
                }
            },
            max_tokens = 300
        };

        string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();

        JObject jsonResponse = JObject.Parse(responseBody);

        Console.WriteLine(responseBody);
        return jsonResponse["choices"][0]["message"]["content"].ToString();

    }

    static string EncodeImageToBase64(string imagePath)
    {
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        return Convert.ToBase64String(imageBytes);
    }
}
