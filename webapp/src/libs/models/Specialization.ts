export interface ISpecialization {
    id: string;
    label: string;
    name: string;
    description: string;
    roleInformation: string;
    indexName: string;
    deployment: string;
    imageFilePath: string;
    iconFilePath: string;
    isActive: boolean;
    groupMemberships: string[];
    strictness: number;
    documentCount: number;
}

export interface ISpecializationRequest {
    label: string;
    name: string;
    description: string;
    roleInformation: string;
    indexName: string;
    deployment: string;
    imageFilePath: string;
    iconFilePath: string;
    groupMemberships: string[];
}

export interface ISpecializationToggleRequest {
    isActive: boolean;
}
