import { StepwiseThoughtProcess } from './StepwiseThoughtProcess';

// The final prompt sent to generate bot response.
export interface BotResponsePrompt {
    // The system persona of the chat.
    systemPersona: string;

    // Audience extracted from conversation history.
    audience: string;

    // User intent extracted from input and conversation history.
    userIntent: string;

    // Chat memories queried from the chat memory store if any, includes long term and working memory.
    chatMemories: string;

    // Relevant additional knowledge extracted using a planner.
    externalInformation: string;

    // Recent messages from history of the conversation.
    chatHistory: string;

    // The collection of context messages associated with this chat completions request.
    // Also serves as the rendered prompt template.
    metaPromptTemplate: ContextMessage[];
}

export const PromptSectionsNameMap: Record<string, string> = {
    systemPersona: 'System Persona',
    audience: 'Audience',
    userIntent: 'User Intent',
    chatMemories: 'Chat Memories',
    externalInformation: 'Planner Results',
    chatHistory: 'Chat History',
};

// Information about semantic dependencies of the prompt.
export interface DependencyDetails {
    // Context of the dependency. This can be either the prompt template or planner details.
    context: string | StepwiseThoughtProcess;

    // Result of the dependency. This is the output that's injected into the prompt.
    result: string;
}

// As defined by ChatRole struct in the Azure OpenAI SDK.
// See https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai.chatrole?view=azure-dotnet-preview.
enum AuthorRoles {
    System = 'System',
    User = 'User',
    Assistant = 'Assistant',
    Tool = 'Tool',
    Function = 'Function',
}

// The collection of context messages associated with this chat completions request.
// See https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai.chatcompletionsoptions.messages?view=azure-dotnet-preview#azure-ai-openai-chatcompletionsoptions-messages.
interface ContextMessage {
    Role: {
        Label: AuthorRoles;
    };
    Content: string;
}
