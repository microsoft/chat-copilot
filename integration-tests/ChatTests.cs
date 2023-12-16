// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using Xunit;
using static CopilotChat.WebApi.Models.Storage.CopilotChatMessage;

namespace ChatCopilotIntegrationTests;

public class ChatTests : ChatCopilotIntegrationTest
{
    [Fact]
    public async void ChatMessagePostSucceedsWithValidInput()
    {
        await this.SetUpAuth();

        // Create chat session
        var createChatParams = new CreateChatParameters() { Title = nameof(ChatMessagePostSucceedsWithValidInput) };
        HttpResponseMessage response = await this._httpClient.PostAsJsonAsync("chats", createChatParams);
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        var createChatResponse = await JsonSerializer.DeserializeAsync<CreateChatResponse>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(createChatResponse);

        // Ask something to the bot
        var ask = new Ask
        {
            Input = "Who is Satya Nadella?",
            Variables = new KeyValuePair<string, string>[] { new("MessageType", ChatMessageType.Message.ToString()) }
        };
        response = await this._httpClient.PostAsJsonAsync($"chats/{createChatResponse.ChatSession.Id}/messages", ask);
        response.EnsureSuccessStatusCode();

        contentStream = await response.Content.ReadAsStreamAsync();
        var askResult = await JsonSerializer.DeserializeAsync<AskResult>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(askResult);
        Assert.False(string.IsNullOrEmpty(askResult.Value));


        // Clean up
        response = await this._httpClient.DeleteAsync($"chats/{createChatResponse.ChatSession.Id}");
        response.EnsureSuccessStatusCode();
    }
}

