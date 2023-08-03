// Copyright (c) Microsoft. All rights reserved.

export interface MemoriesStore {
    types: string[];
    selectedType: string;
}

export interface ServiceOptions {
    memoriesStore: MemoriesStore;
}
