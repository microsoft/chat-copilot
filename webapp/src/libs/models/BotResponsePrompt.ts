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

    // Preamble to the LLM's response.
    systemChatContinuation: string;

    // Raw content of the rendered prompt.
    rawContent: string;
}

export const PromptSectionsNameMap: Record<string, string> = {
    systemPersona: 'System Persona',
    audience: 'Audience',
    userIntent: 'User Intent',
    chatMemories: 'Chat Memories',
    externalInformation: 'Planner Results',
    chatHistory: 'Chat History',
    systemChatContinuation: 'System Chat Continuation',
};

// Information about semantic dependencies of the prompt.
export interface DependencyDetails {
    // Context of the dependency. This can be either the prompt template or planner details.
    context: string | StepwiseThoughtProcess;

    // Result of the dependency. This is the output that's injected into the prompt.
    result: string;
}
