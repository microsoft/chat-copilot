// Copyright (c) Microsoft. All rights reserved.

import { AnyAction, Dispatch } from '@reduxjs/toolkit';
import { AlertType } from '../../../libs/models/AlertType';
import { addAlert } from '../app/appSlice';
import { IChatMessage } from './../../../libs/models/ChatMessage';
import { StoreMiddlewareAPI, getSelectedChatID } from './../../app/store';
import { getOrCreateHubConnection } from './signalRHubConnection';

// The action sent to the SignalR middleware.
interface SignalRAction extends AnyAction {
    payload: {
        message?: IChatMessage;
        userId?: string;
        isTyping?: boolean;
        id?: string;
    };
}

export const signalRMiddleware = (store: StoreMiddlewareAPI) => {
    return (next: Dispatch) => (action: SignalRAction) => {
        // Call the next dispatch method in the middleware chain before performing any async logic
        const result = next(action);

        // Get the SignalR connection instance
        const hubConnection = getOrCreateHubConnection(store);

        // The following actions will be captured by the SignalR middleware and broadcasted to all clients.
        switch (action.type) {
            case 'conversations/addMessageToConversationFromUser':
                hubConnection
                    .invoke(
                        'SendMessageAsync',
                        getSelectedChatID(),
                        store.getState().app.activeUserInfo?.id,
                        action.payload.message,
                    )
                    .catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
            case 'conversations/updateUserIsTyping':
                hubConnection
                    .invoke(
                        'SendUserTypingStateAsync',
                        getSelectedChatID(),
                        action.payload.userId,
                        action.payload.isTyping,
                    )
                    .catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
            case 'conversations/setConversations':
                Promise.all(
                    Object.keys(action.payload).map(async (id) => {
                        await hubConnection.invoke('AddClientToGroupAsync', id);
                    }),
                ).catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
            case 'conversations/addConversation':
                hubConnection
                    .invoke('AddClientToGroupAsync', action.payload.id)
                    .catch((err) => store.dispatch(addAlert({ message: String(err), type: AlertType.Error })));
                break;
        }

        return result;
    };
};
