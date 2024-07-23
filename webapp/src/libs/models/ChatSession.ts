// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from './ChatMessage';
/**
 * The interface has been modified to support specialization
 */
export interface IChatSession {
    id: string;
    title: string;
    systemDescription: string;
    memoryBalance: number;
    enabledPlugins: string[];
    specialization?: IChatSpecialization;
}

export interface IChatSpecialization {
    specializationKey: string;
}

export interface ICreateChatSessionResponse {
    chatSession: IChatSession;
    initialBotMessage: IChatMessage;
}
