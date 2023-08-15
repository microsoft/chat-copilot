// Copyright (c) Microsoft. All rights reserved.

export interface MemoryStore {
    types: string[];
    selectedType: string;
}

export interface ServiceOptions {
    memoryStore: MemoryStore;
    version: string;
}
