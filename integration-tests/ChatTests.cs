// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http.Json;
using System.Text.Json;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using Xunit;
using static CopilotChat.WebApi.Models.Storage.CopilotChatMessage;

namespace ChatCopilotIntegrationTests;

public class ChatTests : ChatCopilotIntegrationTest
{
    private static readonly JsonSerializerOptions jsOpts = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async void ChatMessagePostSucceedsWithValidInput()
    {
        await this.SetUpAuthAsync();

        // Create chat session
        var createChatParams = new CreateChatParameters() { Title = nameof(this.ChatMessagePostSucceedsWithValidInput) };
        HttpResponseMessage response = await this.HTTPClient.PostAsJsonAsync("chats", createChatParams);
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var createChatResponse = await JsonSerializer.DeserializeAsync<CreateChatResponse>(contentStream, jsOpts);
        Assert.NotNull(createChatResponse);

        // Ask something to the bot
        var ask = new Ask
        {
            Input = "Who is Satya Nadella?",
            Variables = new KeyValuePair<string, string>[] { new("MessageType", ChatMessageType.Message.ToString()) }
        };
        response = await this.HTTPClient.PostAsJsonAsync($"chats/{createChatResponse.ChatSession.Id}/messages", ask);
        response.EnsureSuccessStatusCode();

        contentStream = await response.Content.ReadAsStreamAsync();
        var askResult = await JsonSerializer.DeserializeAsync<AskResult>(contentStream, jsOpts);
        Assert.NotNull(askResult);
        Assert.False(string.IsNullOrEmpty(askResult.Value));

        // Clean up
        response = await this.HTTPClient.DeleteAsync($"chats/{createChatResponse.ChatSession.Id}").ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
