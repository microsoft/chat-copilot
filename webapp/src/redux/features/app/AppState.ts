// Copyright (c) Microsoft. All rights reserved.

import { AlertType } from '../../../libs/models/AlertType';
import { TokenUsage } from '../../../libs/models/TokenUsage';

export interface AppState {
    alerts: Alert[];
    activeUserInfo?: ActiveUserInfo;
}

export interface ActiveUserInfo {
    id: string;
    email: string;
    username: string;
}

export interface Alert {
    message: string;
    type: AlertType;
}

export interface Setting {
    title: string;
    description?: string;
    stackVertically?: boolean;
    learnMoreLink?: string;
}

export interface AppState {
    alerts: Alert[];
    activeUserInfo?: ActiveUserInfo;
    // Total usage across all chats by app session
    tokenUsage: TokenUsage;
}

export const initialState: AppState = {
    alerts: [
        {
            message:
                'By using Chat Copilot, you agree to protect sensitive data, not store it in chat, and allow chat history collection for service improvements. This tool is for internal use only.',
            type: AlertType.Info,
        },
    ],
    tokenUsage: {
        prompt: 0,
        dependency: 0,
    },
};
