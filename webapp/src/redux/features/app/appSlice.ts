// Copyright (c) Microsoft. All rights reserved.

import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { TokenUsage } from '../../../libs/models/TokenUsage';
import { ActiveUserInfo, Alert, AppState, initialState } from './AppState';

export const appSlice = createSlice({
    name: 'app',
    initialState,
    reducers: {
        setAlerts: (state: AppState, action: PayloadAction<Alert[]>) => {
            state.alerts = action.payload;
        },
        addAlert: (state: AppState, action: PayloadAction<Alert>) => {
            if (state.alerts.length === 3) {
                state.alerts.shift();
            }
            state.alerts.push(action.payload);
        },
        removeAlert: (state: AppState, action: PayloadAction<number>) => {
            state.alerts.splice(action.payload, 1);
        },
        setActiveUserInfo: (state: AppState, action: PayloadAction<ActiveUserInfo>) => {
            state.activeUserInfo = action.payload;
        },
        updateTokenUsage: (state: AppState, action: PayloadAction<TokenUsage>) => {
            state.tokenUsage = {
                prompt: state.tokenUsage.prompt + action.payload.prompt,
                dependency: state.tokenUsage.dependency + action.payload.dependency,
            };
        },
    },
});

export const { addAlert, removeAlert, setAlerts, setActiveUserInfo, updateTokenUsage } = appSlice.actions;

export default appSlice.reducer;
