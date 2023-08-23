# Chat Copilot Memory Pipeline

> **!IMPORTANT**
> This sample is for educational purposes only and is not recommended for production deployments.

> **IMPORTANT:** The pipeline will call Azure OpenAI/OpenAI which will use tokens that you may be billed for.

## Introduction

### Memory

One of the exciting features of the Copilot Chat App is its ability to store contextual information
to [memories](https://github.com/microsoft/semantic-kernel/blob/main/docs/EMBEDDINGS.md) and retrieve
relevant information from memories to provide more meaningful answers to users through out the conversations.

Memories can be generated from conversations as well as imported from external sources, such as documents.
Importing documents enables Copilot Chat to have up-to-date knowledge of specific contexts, such as enterprise and personal data.

### Memory pipeline in Chat Copilot

Chat copilot integrates [Semantic Memory](https://github.com/microsoft/semantic-memory) as the memory solution provider. The memory pipeline is designed to be run as an asynchronous service. If you are expecting to import big documents that can require minutes to process or planning to carry long conversations with the bot, then you can deploy the memory pipeline as a separate service along with the [chat copilot webapi](https://github.com/microsoft/chat-copilot/tree/main/webapi).

## Configuration

### Local dev environment

#### Webapi

// TODO

#### Memorypipeline

// TODO

### Cloud deployment

#### Webapi

// TODO

#### Memorypipeline

// TODO
