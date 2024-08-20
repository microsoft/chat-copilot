export interface ISpecialization {
    id: string;
    key: string;
    name: string;
    description: string;
    roleInformation: string;
    indexName: string;
    imageFilePath: string;
    isActive: boolean;
    groupMemberships: string[];
    strictness: number;
    documentCount: number;
}

export interface ISpecializationRequest {
    key: string;
    name: string;
    description: string;
    roleInformation: string;
    indexName: string;
    imageFilePath: string;
}

export interface ISpecializationToggleRequest {
    isActive: boolean;
}
