// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../../models/ChatMessage';
import { ISearchValue } from '../../models/SearchResponse';

export interface IAskResult {
    message: IChatMessage;
    variables: ContextVariable[];
}

export interface IAskSearchResult {
    count: number;
    value: ISearchValue[];
}

export interface ContextVariable {
    key: string;
    value: string;
}
