// Copyright (c) Microsoft. All rights reserved.

export interface MemoryStore {
    types: string[];
    selectedType: string;
}

export interface HostedPlugin {
    name: string;
    url: string;
}

export interface ServiceOptions {
    memoryStore: MemoryStore;
    availablePlugins: HostedPlugin[];
    version: string;
}
