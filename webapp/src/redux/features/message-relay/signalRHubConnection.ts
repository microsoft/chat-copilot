// Copyright (c) Microsoft. All rights reserved.

import * as signalR from '@microsoft/signalr';
import { Constants } from '../../../Constants';
import { COPY } from '../../../assets/strings';
import { getFriendlyChatName } from '../../../libs/hooks/useChat';
import { AlertType } from '../../../libs/models/AlertType';
import { AuthorRoles, ChatMessageType, IChatMessage } from '../../../libs/models/ChatMessage';
import { IChatUser } from '../../../libs/models/ChatUser';
import { PlanState } from '../../../libs/models/Plan';
import { BackendServiceUrl } from '../../../libs/services/BaseService';
import { StoreMiddlewareAPI } from '../../app/store';
import { addAlert, setMaintenance } from '../app/appSlice';
import { ChatState } from '../conversations/ChatState';
import { UpdatePluginStatePayload } from '../conversations/ConversationsState';

/*
 * This is a module that encapsulates the SignalR connection
 * to the messageRelayHub on the server.
 */

// These have to match the callback names used in the backend
const enum SignalRCallbackMethods {
    ReceiveMessage = 'ReceiveMessage',
    ReceiveMessageUpdate = 'ReceiveMessageUpdate',
    UserJoined = 'UserJoined',
    ReceiveUserTypingState = 'ReceiveUserTypingState',
    ReceiveBotResponseStatus = 'ReceiveBotResponseStatus',
    GlobalDocumentUploaded = 'GlobalDocumentUploaded',
    ChatEdited = 'ChatEdited',
    ChatDeleted = 'ChatDeleted',
    GlobalSiteMaintenance = 'GlobalSiteMaintenance',
    PluginStateChanged = 'PluginStateChanged',
}

// Set up a SignalR connection to the messageRelayHub on the server
const setupSignalRConnectionToChatHub = () => {
    const connectionHubUrl = new URL('/messageRelayHub', BackendServiceUrl);
    const signalRConnectionOptions = {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        logger: signalR.LogLevel.Warning,
    };

    // Create the connection instance
    // withAutomaticReconnect will automatically try to reconnect and generate a new socket connection if needed
    const hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(connectionHubUrl.toString(), signalRConnectionOptions)
        .withAutomaticReconnect()
        .withHubProtocol(new signalR.JsonHubProtocol())
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Note: to keep the connection open the serverTimeout should be
    // larger than the KeepAlive value that is set on the server
    // keepAliveIntervalInMilliseconds default is 15000 and we are using default
    // serverTimeoutInMilliseconds default is 30000 and we are using 60000 set below
    hubConnection.serverTimeoutInMilliseconds = 60000;

    return hubConnection;
};

const registerCommonSignalConnectionEvents = (hubConnection: signalR.HubConnection, store: StoreMiddlewareAPI) => {
    // Re-establish the connection if connection dropped
    hubConnection.onclose((error) => {
        if (hubConnection.state === signalR.HubConnectionState.Disconnected) {
            const errorMessage = 'Connection closed due to error. Try refreshing this page to restart the connection';
            store.dispatch(
                addAlert({
                    message: String(errorMessage),
                    type: AlertType.Error,
                    id: Constants.app.CONNECTION_ALERT_ID,
                }),
            );
            console.log(errorMessage, error);
        }
    });

    hubConnection.onreconnecting((error) => {
        if (hubConnection.state === signalR.HubConnectionState.Reconnecting) {
            const errorMessage = 'Connection lost due to error. Reconnecting...';
            store.dispatch(
                addAlert({
                    message: String(errorMessage),
                    type: AlertType.Info,
                    id: Constants.app.CONNECTION_ALERT_ID,
                }),
            );
            console.log(errorMessage, error);
        }
    });

    hubConnection.onreconnected((connectionId = '') => {
        if (hubConnection.state === signalR.HubConnectionState.Connected) {
            const message = 'Connection reestablished. Please refresh the page to ensure you have the latest data.';
            store.dispatch(addAlert({ message, type: AlertType.Success, id: Constants.app.CONNECTION_ALERT_ID }));
            console.log(message + ` Connected with connectionId ${connectionId}`);
        }
    });
};

const startSignalRConnection = (hubConnection: signalR.HubConnection, store: StoreMiddlewareAPI) => {
    registerCommonSignalConnectionEvents(hubConnection, store);
    hubConnection
        .start()
        .then(() => {
            console.assert(hubConnection.state === signalR.HubConnectionState.Connected);
            console.log('SignalR connection established');
        })
        .catch((err) => {
            console.assert(hubConnection.state === signalR.HubConnectionState.Disconnected);
            console.error('SignalR Connection Error: ', err);
            setTimeout(() => {
                startSignalRConnection(hubConnection, store);
            }, 5000);
        });
};

const registerSignalREvents = (hubConnection: signalR.HubConnection, store: StoreMiddlewareAPI) => {
    hubConnection.on(
        SignalRCallbackMethods.ReceiveMessage,
        (chatId: string, senderId: string, message: IChatMessage) => {
            if (message.authorRole === AuthorRoles.Bot) {
                const loggedInUserId = store.getState().app.activeUserInfo?.id;
                const responseToLoggedInUser = loggedInUserId === senderId;
                message.planState =
                    message.type === ChatMessageType.Plan && responseToLoggedInUser
                        ? PlanState.PlanApprovalRequired
                        : PlanState.Disabled;
            }

            store.dispatch({ type: 'conversations/addMessageToConversationFromServer', payload: { chatId, message } });
        },
    );

    hubConnection.on(SignalRCallbackMethods.ReceiveMessageUpdate, (message: IChatMessage) => {
        const { chatId, id: messageId, content } = message;
        // If tokenUsage is defined, that means full message content has already been streamed and updated from server. No need to update content again.
        store.dispatch({
            type: 'conversations/updateMessageProperty',
            payload: {
                chatId,
                messageIdOrIndex: messageId,
                property: message.tokenUsage ? 'tokenUsage' : 'content',
                value: message.tokenUsage ?? content,
                frontLoad: true,
            },
        });
    });

    hubConnection.on(SignalRCallbackMethods.UserJoined, (chatId: string, userId: string) => {
        const user: IChatUser = {
            id: userId,
            online: false,
            fullName: '',
            emailAddress: '',
            isTyping: false,
            photo: '',
        };
        store.dispatch({ type: 'conversations/addUserToConversation', payload: { user, chatId } });
    });

    hubConnection.on(
        SignalRCallbackMethods.ReceiveUserTypingState,
        (chatId: string, userId: string, isTyping: boolean) => {
            store.dispatch({
                type: 'conversations/updateUserIsTypingFromServer',
                payload: { chatId, userId, isTyping },
            });
        },
    );

    hubConnection.on(SignalRCallbackMethods.ReceiveBotResponseStatus, (chatId: string, status: string) => {
        store.dispatch({ type: 'conversations/updateBotResponseStatus', payload: { chatId, status } });
    });

    hubConnection.on(SignalRCallbackMethods.GlobalDocumentUploaded, (fileNames: string, userName: string) => {
        store.dispatch(addAlert({ message: `${userName} uploaded ${fileNames} to all chats`, type: AlertType.Info }));
    });

    hubConnection.on(SignalRCallbackMethods.ChatEdited, (chat: ChatState) => {
        const { id, title } = chat;
        if (!(id in store.getState().conversations.conversations)) {
            store.dispatch(
                addAlert({
                    message: `Chat ${id} not found in store. Chat edited signal from server is not processed.`,
                    type: AlertType.Error,
                }),
            );
        }
        store.dispatch({ type: 'conversations/editConversationTitle', payload: { id, newTitle: title } });
    });

    // User Id is that of the user who initiated the deletion.
    hubConnection.on(SignalRCallbackMethods.ChatDeleted, (chatId: string, userId: string) => {
        const conversations = store.getState().conversations.conversations;
        if (!(chatId in conversations)) {
            store.dispatch({
                message: `Chat ${chatId} not found in store. ChatDeleted signal from server was not processed. ${COPY.REFRESH_APP_ADVISORY}`,
                type: AlertType.Error,
            });
        } else {
            const friendlyChatName = getFriendlyChatName(conversations[chatId]);
            const deletedByAnotherUser = userId !== store.getState().app.activeUserInfo?.id;

            store.dispatch(
                addAlert({
                    message: deletedByAnotherUser
                        ? COPY.CHAT_DELETED_MESSAGE(friendlyChatName)
                        : `Chat {${friendlyChatName}} deleted successfully.`,
                    type: AlertType.Warning,
                }),
            );

            if (deletedByAnotherUser)
                store.dispatch({
                    type: 'conversations/disableConversation',
                    payload: chatId,
                });
        }
    });

    hubConnection.on(SignalRCallbackMethods.GlobalSiteMaintenance, () => {
        store.dispatch(setMaintenance(true));
    });

    hubConnection.on(
        SignalRCallbackMethods.PluginStateChanged,
        (chatId: string, pluginName: string, pluginState: boolean) => {
            store.dispatch({
                type: 'conversations/updatePluginState',
                payload: { id: chatId, pluginName: pluginName, newState: pluginState } as UpdatePluginStatePayload,
            });
        },
    );
};

// This is a singleton instance of the SignalR connection
let hubConnection: signalR.HubConnection | undefined = undefined;

// This function will return the singleton instance of the SignalR connection
export const getOrCreateHubConnection = (store: StoreMiddlewareAPI) => {
    if (hubConnection === undefined) {
        hubConnection = setupSignalRConnectionToChatHub();

        // Start the signalR connection to make sure messages are
        // sent to all clients and received by all clients
        startSignalRConnection(hubConnection, store);
        registerSignalREvents(hubConnection, store);
    }
    return hubConnection;
};
