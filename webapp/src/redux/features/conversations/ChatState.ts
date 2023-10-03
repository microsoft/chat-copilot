// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../../../libs/models/ChatMessage';
import { IChatUser } from '../../../libs/models/ChatUser';

export interface ChatState {
    id: string;
    title: string;
    systemDescription: string;
    memoryBalance: number;
    users: IChatUser[];
    messages: IChatMessage[];
    enabledHostedPlugins: string[];
    botProfilePicture: string;
    lastUpdatedTimestamp?: number;
    input: string;
    botResponseStatus: string | undefined;
    userDataLoaded: boolean;
    importingDocuments?: string[];
    disabled: boolean; // For labeling a chat has been deleted
    hidden: boolean; // For hiding a chat from the list
}
