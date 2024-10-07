// Copyright (c) Microsoft. All rights reserved.

import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import { IChatUser } from '../../../libs/models/ChatUser';
import { IAskResult } from '../../../libs/semantic-kernel/model/AskResult';
import { ChatState } from './ChatState';
import {
    ConversationInputChange,
    Conversations,
    ConversationSpecializationChange,
    ConversationsState,
    ConversationSuggestionsChange,
    ConversationSystemDescriptionChange,
    ConversationTitleChange,
    initialState,
    UpdatePluginStatePayload,
} from './ConversationsState';

export const conversationsSlice = createSlice({
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
        editConversationSpecialization: (
            state: ConversationsState,
            action: PayloadAction<ConversationSpecializationChange>,
        ) => {
            const id = action.payload.id;
            state.conversations[id].specializationId = action.payload.specializationId;
            frontLoadChat(state, id);
        },
        editConversationInput: (state: ConversationsState, action: PayloadAction<ConversationInputChange>) => {
            const id = action.payload.id;
            const newInput = action.payload.newInput;
            state.conversations[id].input = newInput;
        },
        editConversationSystemDescription: (
            state: ConversationsState,
            action: PayloadAction<ConversationSystemDescriptionChange>,
        ) => {
            const id = action.payload.id;
            const newSystemDescription = action.payload.newSystemDescription;
            state.conversations[id].systemDescription = newSystemDescription;
        },
        editConversationMemoryBalance: (
            state: ConversationsState,
            action: PayloadAction<{ id: string; memoryBalance: number }>,
        ) => {
            const id = action.payload.id;
            const newMemoryBalance = action.payload.memoryBalance;
            state.conversations[id].memoryBalance = newMemoryBalance;
        },
        setSelectedConversation: (state: ConversationsState, action: PayloadAction<string>) => {
            state.selectedId = action.payload;
        },
        toggleMultiUserConversations: (state: ConversationsState) => {
            const keys = Object.keys(state.conversations);
            keys.forEach((key) => {
                if (state.conversations[key].users.length > 1) {
                    state.conversations[key].hidden = !state.conversations[key].hidden;
                }
            });
        },
        addConversation: (state: ConversationsState, action: PayloadAction<ChatState>) => {
            const newId = action.payload.id;
            state.conversations = { ...state.conversations, [newId]: action.payload };
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
        setImportingDocumentsToConversation: (
            state: ConversationsState,
            action: PayloadAction<{ importingDocuments: string[]; chatId: string }>,
        ) => {
            const { importingDocuments, chatId } = action.payload;
            state.conversations[chatId].importingDocuments = importingDocuments;
        },
        setUsersLoaded: (state: ConversationsState, action: PayloadAction<string>) => {
            state.conversations[action.payload].userDataLoaded = true;
        },
        /*
         * addMessageToConversationFromUser() and addMessageToConversationFromServer() both update the conversations state.
         * However they are for different purposes. The former action is for updating the conversation from the
         * webapp and will be captured by the SignalR middleware and the payload will be broadcasted to all clients
         * in the same group.
         * The addMessageToConversationFromServer() action is triggered by the SignalR middleware when a response is received
         * from the webapi.
         */
        addMessageToConversationFromUser: (
            state: ConversationsState,
            action: PayloadAction<{ message: IChatMessage; chatId: string }>,
        ) => {
            const { message, chatId } = action.payload;
            updateConversation(state, chatId, message);
        },
        addMessageToConversationFromServer: (
            state: ConversationsState,
            action: PayloadAction<{ message: IChatMessage; chatId: string }>,
        ) => {
            const { message, chatId } = action.payload;
            updateConversation(state, chatId, message);
        },
        /*
         * updateUserIsTyping() and updateUserIsTypingFromServer() both update a user's typing state.
         * However they are for different purposes. The former action is for updating an user's typing state from
         * the webapp and will be captured by the SignalR middleware and the payload will be broadcasted to all clients
         * in the same group.
         * The updateUserIsTypingFromServer() action is triggered by the SignalR middleware when a state is received
         * from the webapi.
         */
        updateUserIsTyping: (
            state: ConversationsState,
            action: PayloadAction<{ userId: string; chatId: string; isTyping: boolean }>,
        ) => {
            const { userId, chatId, isTyping } = action.payload;
            updateUserTypingState(state, userId, chatId, isTyping);
        },
        updateUserIsTypingFromServer: (
            state: ConversationsState,
            action: PayloadAction<{ userId: string; chatId: string; isTyping: boolean }>,
        ) => {
            const { userId, chatId, isTyping } = action.payload;
            updateUserTypingState(state, userId, chatId, isTyping);
        },
        updateBotResponseStatus: (
            state: ConversationsState,
            action: PayloadAction<{ chatId: string; status: string | undefined }>,
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
                updatedContent?: string;
                frontLoad?: boolean;
            }>,
        ) => {
            const { property, value, messageIdOrIndex, chatId, updatedContent, frontLoad } = action.payload;
            const conversation = state.conversations[chatId];
            const conversationMessage =
                typeof messageIdOrIndex === 'number'
                    ? conversation.messages[messageIdOrIndex]
                    : conversation.messages.find((m) => m.id === messageIdOrIndex);

            if (conversationMessage) {
                conversationMessage[property] = value;
                if (updatedContent) {
                    conversationMessage.content = updatedContent;
                }
            }

            if (frontLoad) {
                frontLoadChat(state, chatId);
            }
        },
        deleteConversation: (state: ConversationsState, action: PayloadAction<string>) => {
            const keys = Object.keys(state.conversations);
            const id = action.payload;

            // If the conversation being deleted is the selected conversation, select the first remaining conversation
            if (id === state.selectedId) {
                if (keys.length > 1) {
                    state.selectedId = id === keys[0] ? keys[1] : keys[0];
                } else {
                    state.selectedId = '';
                }
            }

            const { [id]: _, ...rest } = state.conversations;
            state.conversations = rest;
        },
        disableConversation: (state: ConversationsState, action: PayloadAction<string>) => {
            const id = action.payload;
            state.conversations[id].disabled = true;
            frontLoadChat(state, id);
            return;
        },
        deleteConversationHistory: (
            state: ConversationsState,
            action: PayloadAction<{ message: IChatMessage; chatId: string }>,
        ) => {
            const { message, chatId } = action.payload;
            // Remove all messages in the conversation
            state.conversations[chatId].messages = [];
            // Insert "Chat History deleted" message into the conversation
            updateConversation(state, chatId, message);
        },
        updatePluginState: (state: ConversationsState, action: PayloadAction<UpdatePluginStatePayload>) => {
            const { id, pluginName, newState } = action.payload;
            const isPluginEnabled = state.conversations[id].enabledHostedPlugins.find((p) => p === pluginName);
            if (newState) {
                if (isPluginEnabled) {
                    return;
                }
                state.conversations[id].enabledHostedPlugins.push(pluginName);
            } else {
                if (!isPluginEnabled) {
                    return;
                }
                state.conversations[id].enabledHostedPlugins = state.conversations[id].enabledHostedPlugins.filter(
                    (p) => p !== pluginName,
                );
            }
        },
        updateSuggestions: (state: ConversationsState, action: PayloadAction<ConversationSuggestionsChange>) => {
            setConversationSuggestions(state, action.payload.id, action.payload.chatSuggestionMessage);
        },
    },
});

const frontLoadChat = (state: ConversationsState, id: string) => {
    const conversation = state.conversations[id];
    const { [id]: _, ...rest } = state.conversations;
    state.conversations = { [id]: conversation, ...rest };
};

const updateConversation = (state: ConversationsState, chatId: string, message: IChatMessage) => {
    state.conversations[chatId].messages.push(message);
    frontLoadChat(state, chatId);
};

const updateUserTypingState = (state: ConversationsState, userId: string, chatId: string, isTyping: boolean) => {
    const conversation = state.conversations[chatId];
    const user = conversation.users.find((u) => u.id === userId);
    if (user) {
        user.isTyping = isTyping;
    }
};

/**
 * Small helper function for attempting to convert a JSON formatted string to a JS string array.
 * Returns an empty array instead of throwing if anything fails.
 * @param str JSON string
 * @returns {string[]}
 */
const extractJsonArray = (str: string) => {
    try {
        const parsed = JSON.parse(str) as unknown;
        if (Array.isArray(parsed) && parsed.every((item) => typeof item === 'string')) {
            return parsed;
        } else {
            return [];
        }
    } catch (e) {
        return [];
    }
};

/**
 * setConversationSuggestions - After asking the bot for some suggested chat topics, this function
 * will attempt to parse the answer as a JSON array and convert it to a valid JS string array object
 * which will be stored in the conversation state.
 *
 * @param state current conversation state
 * @param chatId current chatId guid
 * @param chatMessage the chat message we got in response from the chatbot
 */
const setConversationSuggestions = (state: ConversationsState, chatId: string, chatMessage: IAskResult) => {
    const conversation = state.conversations[chatId];
    let arraySuggestions: string[] = [];
    const response = chatMessage.variables.find((a) => a.key === 'input');
    if (!response) {
        return;
    }
    arraySuggestions = extractJsonArray(response.value); //First try to convert from the raw string.
    if (!arraySuggestions.length) {
        //Sometimes the bot will reply with other text and json wrapped in ```json ... ```
        //so we can try that if the first attempt didn't give us anything.
        const regex = /```json\s*(\[[\s\S]*?\])\s*```/g;
        const match = regex.exec(response.value);
        if (match) {
            arraySuggestions = extractJsonArray(match[1]);
        }
    }
    conversation.suggestions = arraySuggestions;
};

export const {
    setConversations,
    editConversationTitle,
    editConversationInput,
    editConversationSystemDescription,
    editConversationMemoryBalance,
    setSelectedConversation,
    toggleMultiUserConversations,
    addConversation,
    setImportingDocumentsToConversation,
    addMessageToConversationFromUser,
    addMessageToConversationFromServer,
    updateMessageProperty,
    updateUserIsTyping,
    updateUserIsTypingFromServer,
    updateBotResponseStatus,
    setUsersLoaded,
    deleteConversation,
    disableConversation,
    updatePluginState,
    editConversationSpecialization,
    updateSuggestions,
    deleteConversationHistory,
} = conversationsSlice.actions;

export default conversationsSlice.reducer;
