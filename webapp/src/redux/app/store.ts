// Copyright (c) Microsoft. All rights reserved.

import {
    Action,
    Dispatch,
    MiddlewareAPI,
    ThunkMiddleware,
    Tuple,
    UnknownAction,
    configureStore,
} from '@reduxjs/toolkit';
import { AppState } from '../features/app/AppState';
import { ConversationsState } from '../features/conversations/ConversationsState';
import { signalRMiddleware } from '../features/message-relay/signalRMiddleware';
import { PluginsState } from '../features/plugins/PluginsState';
import { UsersState } from '../features/users/UsersState';
import resetStateReducer, { resetApp } from './rootReducer';

export type StoreMiddlewareAPI = MiddlewareAPI<Dispatch, RootState>;
export type Store = typeof store;
export const store = configureStore<RootState, Action, Tuple<Array<ThunkMiddleware<RootState, UnknownAction>>>>({
    reducer: resetStateReducer,
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(signalRMiddleware),
});

export interface RootState {
    app: AppState;
    conversations: ConversationsState;
    plugins: PluginsState;
    users: UsersState;
}

export const getSelectedChatID = (): string => {
    return store.getState().conversations.selectedId;
};

export type AppDispatch = typeof store.dispatch;

// Function to reset the app state
export const resetState = () => {
    store.dispatch(resetApp());
};
