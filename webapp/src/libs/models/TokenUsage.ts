/// Information about token usage used to generate bot response.
export interface TokenUsage {
    /// Total token usage of prompt chat completion.
    prompt: number;
    /// Total token usage across all semantic dependencies used to generate prompt.
    dependency: number;
}

export enum TokenUsageKeys {
    prompt = 'promptTokenUsage',
    dependency = 'dependencyTokenUsage',
}
