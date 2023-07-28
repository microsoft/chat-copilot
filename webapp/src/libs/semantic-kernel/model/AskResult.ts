// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../../models/ChatMessage';

export interface IAskResult {
    message: IChatMessage;
    variables: ContextVariable[];
}

export interface ContextVariable {
    key: string;
    value: string;
}
