// Copyright (c) Microsoft. All rights reserved.

import { Action, Dispatch, Middleware } from '@reduxjs/toolkit';
import { AlertType } from '../../../libs/models/AlertType';
import { addAlert } from '../app/appSlice';
import { IChatMessage } from './../../../libs/models/ChatMessage';
import { RootState, StoreMiddlewareAPI, getSelectedChatID } from './../../app/store';
import { getOrCreateHubConnection } from './signalRHubConnection';

// The action sent to the SignalR middleware.
interface SignalRAction extends Action {
    payload: {
        message?: IChatMessage;
        userId?: string;
        isTyping?: boolean;
        id?: string;
    };
}

export const signalRMiddleware: Middleware<any, RootState, Dispatch<SignalRAction>> = (store: StoreMiddlewareAPI) => {
    return (next) => (action) => {
        // Call the next dispatch method in the middleware chain before performing any async logic
        const signalRAction = action as SignalRAction;
        const result = next(signalRAction);

        // Get the SignalR connection instance
        const hubConnection = getOrCreateHubConnection(store);

        // The following actions will be captured by the SignalR middleware and broadcasted to all clients.
        switch (signalRAction.type) {
            case 'conversations/addMessageToConversationFromUser':
                hubConnection
                    .invoke(
                        'SendMessageAsync',
                        getSelectedChatID(),
                        store.getState().app.activeUserInfo?.id,
                        signalRAction.payload.message,
                    )
                    .catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
            case 'conversations/updateUserIsTyping':
                hubConnection
                    .invoke(
                        'SendUserTypingStateAsync',
                        getSelectedChatID(),
                        signalRAction.payload.userId,
                        signalRAction.payload.isTyping,
                    )
                    .catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
            case 'conversations/setConversations':
                Promise.all(
                    Object.keys(signalRAction.payload).map(async (id) => {
                        await hubConnection.invoke('AddClientToGroupAsync', id);
                    }),
                ).catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
            case 'conversations/addConversation':
                hubConnection
                    .invoke('AddClientToGroupAsync', signalRAction.payload.id)
                    .catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
        }

        return result;
    };
};
