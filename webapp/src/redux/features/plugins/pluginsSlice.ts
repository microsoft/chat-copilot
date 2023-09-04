// Copyright (c) Microsoft. All rights reserved.

import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { BuiltInPlugins, EnablePluginPayload, initialState, Plugin, PluginsState } from './PluginsState';

export const pluginsState = createSlice({
    name: 'plugins',
    initialState,
    reducers: {
        connectPlugin: (state: PluginsState, action: PayloadAction<EnablePluginPayload>) => {
            const plugin: Plugin = state.plugins[action.payload.plugin];
            let authData = action.payload.accessToken;

            switch (action.payload.plugin) {
                case BuiltInPlugins.Jira:
                    authData = `${action.payload.email as string}:${action.payload.accessToken as string}`;
                    break;
                default:
                    // Plugins requiring no auth
                    if (!authData || authData === '') {
                        authData = `${plugin.headerTag}-auth-data`;
                    }
            }

            plugin.enabled = true;
            plugin.authData = authData;
            plugin.apiProperties = action.payload.apiProperties;
        },
        disconnectPlugin: (state: PluginsState, action: PayloadAction<string>) => {
            const plugin = state.plugins[action.payload];
            plugin.enabled = false;
            plugin.authData = undefined;
            const apiProperties = plugin.apiProperties;
            if (apiProperties) {
                Object.keys(apiProperties).forEach((key) => {
                    apiProperties[key].value = undefined;
                });
            }
        },
        addPlugin: (state: PluginsState, action: PayloadAction<Plugin>) => {
            const newId = action.payload.name;
            state.plugins = { [newId]: action.payload, ...state.plugins };
        },
    },
});

export const { connectPlugin, disconnectPlugin, addPlugin } = pluginsState.actions;

export default pluginsState.reducer;
