// Copyright (c) Microsoft. All rights reserved.

import { PlanState } from './Plan';

/**
 * Role of the author of a chat message. It's a copy of AuthorRoles in the API C# code.
 */
export enum AuthorRoles {
    // The current user of the chat.
    User = 0,

    // The bot.
    Bot,

    // The participant who is not the current user nor the bot of the chat.
    Participant,
}

/**
 * Type of the chat message. A copy of ChatMessageType in the API C# code.
 */
export enum ChatMessageType {
    // A message containing text
    Message,

    // A message for a Plan
    Plan,

    // A message showing an uploaded document
    Document,
}

/**
 * States for RLHF
 */
export enum UserFeedback {
    Unknown,
    Requested,
    Positive,
    Negative,
}

export interface IChatMessage {
    type: ChatMessageType;
    timestamp: number;
    userName: 'bot' | string;
    userId: string;
    content: string;
    id?: string;
    prompt?: string;
    authorRole: AuthorRoles;
    debug?: string;
    planState?: PlanState;
    // TODO: [Issue #42] Persistent RLHF
    userFeedback?: UserFeedback;
}
