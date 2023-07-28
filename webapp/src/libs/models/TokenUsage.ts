/// Information about token usage used to generate bot response.
export type TokenUsage = Record<string, number | undefined>;

export type TokenUsageView = Record<string, TokenUsageViewDetails>;

export interface TokenUsageViewDetails {
    usageCount: number;
    legendLabel: string;
    dependency: boolean;
    color: string;
}

export interface FunctionDetails {
    usageCount: number;
    legendLabel: string;
    color?: string;
}

export const TokenUsageFunctionNameMap: Record<string, string> = {
    audienceExtraction: 'Audience Extraction',
    userIntentExtraction: 'User Intent Extraction',
    metaPromptTemplate: 'Meta Prompt Template',
    workingMemoryExtraction: 'Working Memory Extraction',
    longTermMemoryExtraction: 'Long Term Memory Extraction',
    responseCompletion: 'Response Completion',
};
