// Copyright (c) Microsoft. All rights reserved.

import { AuthHelper, DefaultActiveUserInfo } from '../../../libs/auth/AuthHelper';
import { AlertType } from '../../../libs/models/AlertType';
import { ServiceOptions } from '../../../libs/models/ServiceOptions';
import { TokenUsage } from '../../../libs/models/TokenUsage';

export interface ActiveUserInfo {
    id: string;
    email: string;
    username: string;
}

export interface Alert {
    message: string;
    type: AlertType;
    id?: string;
    onRetry?: () => void;
}

interface Feature {
    enabled: boolean; // Whether to show the feature in the UX
    label: string;
    inactive?: boolean; // Set to true if you don't want the user to control the visibility of this feature or there's no backend support
    description?: string;
}

export interface Setting {
    title: string;
    description?: string;
    features: FeatureKeys[];
    stackVertically?: boolean;
    learnMoreLink?: string;
}

export interface AppState {
    alerts: Alert[];
    activeUserInfo?: ActiveUserInfo;
    tokenUsage: TokenUsage;
    features: Record<FeatureKeys, Feature>;
    settings: Setting[];
    serviceOptions: ServiceOptions;
    isMaintenance: boolean;
}

export enum FeatureKeys {
    DarkMode,
    SimplifiedExperience,
    PluginsPlannersAndPersonas,
    AzureContentSafety,
    AzureCognitiveSearch,
    BotAsDocs,
    MultiUserChat,
    RLHF, // Reinforcement Learning from Human Feedback
}

export const Features = {
    [FeatureKeys.DarkMode]: {
        enabled: false,
        label: 'Dark Mode',
    },
    [FeatureKeys.SimplifiedExperience]: {
        enabled: true,
        label: 'Simplified Chat Experience',
    },
    [FeatureKeys.PluginsPlannersAndPersonas]: {
        enabled: true,
        label: 'Plugins & Planners & Personas',
        description: 'The Plans and Persona tabs are hidden until you turn this on',
    },
    [FeatureKeys.AzureContentSafety]: {
        enabled: false,
        label: 'Azure Content Safety',
        inactive: true,
    },
    [FeatureKeys.AzureCognitiveSearch]: {
        enabled: false,
        label: 'Azure Cognitive Search',
        inactive: true,
    },
    [FeatureKeys.BotAsDocs]: {
        enabled: false,
        label: 'Export Chat Sessions',
    },
    [FeatureKeys.MultiUserChat]: {
        enabled: false,
        label: 'Live Chat Session Sharing',
        description: 'Enable multi-user chat sessions. Not available when authorization is disabled.',
        inactive: !AuthHelper.IsAuthAAD,
    },
    [FeatureKeys.RLHF]: {
        enabled: false,
        label: 'Reinforcement Learning from Human Feedback',
        description: 'Enable users to vote on model-generated responses. For demonstration purposes only.',
        // TODO: [Issue #42] Send and store feedback in backend
    },
};

export const Settings = [
    {
        // Basic settings has to stay at the first index. Add all new settings to end of array.
        title: 'Basic',
        features: [FeatureKeys.DarkMode, FeatureKeys.PluginsPlannersAndPersonas],
        stackVertically: true,
    },
    {
        title: 'Display',
        features: [FeatureKeys.SimplifiedExperience],
        stackVertically: true,
    },
    {
        title: 'Azure AI',
        features: [FeatureKeys.AzureContentSafety, FeatureKeys.AzureCognitiveSearch],
        stackVertically: true,
    },
    {
        title: 'Experimental',
        description: 'The related icons and menu options are hidden until you turn this on',
        features: [FeatureKeys.BotAsDocs, FeatureKeys.MultiUserChat, FeatureKeys.RLHF],
    },
];

export const initialState: AppState = {
    alerts: [],
    activeUserInfo: AuthHelper.IsAuthAAD ? undefined : DefaultActiveUserInfo,
    tokenUsage: {},
    features: Features,
    settings: Settings,
    serviceOptions: { memoryStore: { types: [], selectedType: '' }, version: '' },
    isMaintenance: false,
};
