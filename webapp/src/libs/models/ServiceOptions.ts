// Copyright (c) Microsoft. All rights reserved.

export interface MemoriesStoreType {
    types: string[];
    selectedType: string;
}

export interface ServiceOptions {
    memoriesStoreType: MemoriesStoreType;
}
