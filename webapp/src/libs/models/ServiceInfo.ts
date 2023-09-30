// Copyright (c) Microsoft. All rights reserved.

export interface MemoryStore {
    types: string[];
    selectedType: string;
}

export interface ServiceInfo {
    memoryStore: MemoryStore;
    version: string;
    isContentSafetyEnabled: boolean;
}
