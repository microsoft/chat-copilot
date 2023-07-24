// Copyright (c) Microsoft. All rights reserved.

import { createSlice, PayloadAction, Slice } from '@reduxjs/toolkit';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import { IChatUser } from '../../../libs/models/ChatUser';
import { ChatState } from './ChatState';
import {
    ConversationInputChange,
    Conversations,
    ConversationsState,
    ConversationTitleChange,
    initialState,
} from './ConversationsState';

export const conversationsSlice: Slice<ConversationsState> = createSlice({
    name: 'conversations',
    initialState,
    reducers: {
        setConversations: (state: ConversationsState, action: PayloadAction<Conversations>) => {
            state.conversations = action.payload;
        },
        editConversationTitle: (state: ConversationsState, action: PayloadAction<ConversationTitleChange>) => {
            const id = action.payload.id;
            const newTitle = action.payload.newTitle;
            state.conversations[id].title = newTitle;
            frontLoadChat(state, id);
        },
        editConversationInput: (state: ConversationsState, action: PayloadAction<ConversationInputChange>) => {
            const id = action.payload.id;
            const newInput = action.payload.newInput;
            state.conversations[id].input = newInput;
        },
        setSelectedConversation: (state: ConversationsState, action: PayloadAction<string>) => {
            state.selectedId = action.payload;
        },
        addConversation: (state: ConversationsState, action: PayloadAction<ChatState>) => {
            const newId = action.payload.id;
            state.conversations = { [newId]: action.payload, ...state.conversations };
            state.selectedId = newId;
        },
        addUserToConversation: (
            state: ConversationsState,
            action: PayloadAction<{ user: IChatUser; chatId: string }>,
        ) => {
            const { user, chatId } = action.payload;
            state.conversations[chatId].users.push(user);
            state.conversations[chatId].userDataLoaded = false;
        },
        setUsersLoaded: (state: ConversationsState, action: PayloadAction<string>) => {
            state.conversations[action.payload].userDataLoaded = true;
        },
        addMessageToConversation: (
            state: ConversationsState,
            action: PayloadAction<{ message: IChatMessage; chatId: string }>,
        ) => {
            const { message, chatId } = action.payload;
            state.conversations[chatId].messages.push(message);
            frontLoadChat(state, chatId);
        },
        updateUserIsTyping: (
            state: ConversationsState,
            action: PayloadAction<{ userId: string; chatId: string; isTyping: boolean }>,
        ) => {
            const { userId, chatId, isTyping } = action.payload;
            updateUserTypingState(state, userId, chatId, isTyping);
        },
        updateBotResponseStatus: (
            state: ConversationsState,
            action: PayloadAction<{ chatId: string; status: string }>,
        ) => {
            const { chatId, status } = action.payload;
            const conversation = state.conversations[chatId];
            conversation.botResponseStatus = status;
        },
        updateMessageProperty: <K extends keyof IChatMessage, V extends IChatMessage[K]>(
            state: ConversationsState,
            action: PayloadAction<{
                property: K;
                value: V;
                chatId: string;
                messageIdOrIndex: string | number;
                frontLoad?: boolean;
            }>,
        ) => {
            const { property, value, messageIdOrIndex, chatId, frontLoad } = action.payload;
            const conversation = state.conversations[chatId];
            const conversationMessage =
                typeof messageIdOrIndex === 'number'
                    ? conversation.messages[messageIdOrIndex]
                    : conversation.messages.find((m) => m.id === messageIdOrIndex);

            if (conversationMessage) {
                conversationMessage[property] = value;
            }
            if (frontLoad) {
                frontLoadChat(state, chatId);
            }
        },
    },
});

export const {
    setConversations,
    editConversationTitle,
    editConversationInput,
    setSelectedConversation,
    addConversation,
    addMessageToConversation,
    updateConversationFromServer,
    updateMessageProperty,
    updateUserIsTyping,
    updateUserIsTypingFromServer,
    updateBotResponseStatus,
    setUsersLoaded,
} = conversationsSlice.actions;

export default conversationsSlice.reducer;

const frontLoadChat = (state: ConversationsState, id: string) => {
    const conversation = state.conversations[id];
    const { [id]: _, ...rest } = state.conversations;
    state.conversations = { [id]: conversation, ...rest };
};

const updateUserTypingState = (state: ConversationsState, userId: string, chatId: string, isTyping: boolean) => {
    const conversation = state.conversations[chatId];
    const user = conversation.users.find((u) => u.id === userId);
    if (user) {
        user.isTyping = isTyping;
    }
};
