// Copyright (c) Microsoft. All rights reserved.

export interface IAsk {
    input: string;
    variables?: IAskVariables[];
}

export interface IAskVariables {
    key: string;
    value: string;
}

export interface IAskSearch {
    specializationKey: string;
    search: string;
}
