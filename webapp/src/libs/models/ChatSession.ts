// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from './ChatMessage';

export interface IChatSession {
    id: string;
    title: string;
    initialBotMessage?: IChatMessage;
    systemDescription: string;
    memoryBalance: number;
}
