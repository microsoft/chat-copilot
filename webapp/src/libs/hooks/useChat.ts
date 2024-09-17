// Copyright (c) Microsoft. All rights reserved.

import { useMsal } from '@azure/msal-react';
import { Constants } from '../../Constants';
import botIcon1 from '../../assets/bot-icons/bot-icon-1.png';
import { getErrorDetails } from '../../components/utils/TextUtils';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { FeatureKeys } from '../../redux/features/app/AppState';
import { addAlert, toggleFeatureState, updateTokenUsage } from '../../redux/features/app/appSlice';
import { ChatState } from '../../redux/features/conversations/ChatState';
import { Conversations } from '../../redux/features/conversations/ConversationsState';
import {
    addConversation,
    addMessageToConversationFromUser,
    deleteConversation,
    editConversationSpecialization,
    setConversations,
    setSelectedConversation,
    updateBotResponseStatus,
} from '../../redux/features/conversations/conversationsSlice';
import { Plugin } from '../../redux/features/plugins/PluginsState';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { ChatArchive } from '../models/ChatArchive';
import { AuthorRoles, ChatMessageType, IChatMessage } from '../models/ChatMessage';
import { IChatSession, ICreateChatSessionResponse } from '../models/ChatSession';
import { IChatUser } from '../models/ChatUser';
import { PlanState } from '../models/Plan';
import { ISpecialization } from '../models/Specialization';
import { TokenUsage } from '../models/TokenUsage';
import { IAskVariables } from '../semantic-kernel/model/Ask';
import { ContextVariable } from '../semantic-kernel/model/AskResult';
import { ChatArchiveService } from '../services/ChatArchiveService';
import { ChatService } from '../services/ChatService';
import { DocumentImportService } from '../services/DocumentImportService';

export interface GetResponseOptions {
    messageType: ChatMessageType;
    value: string;
    chatId: string;
    kernelArguments?: IAskVariables[];
    processPlan?: boolean;
}

export const useChat = () => {
    const dispatch = useAppDispatch();
    const { specializations: specializationsState } = useAppSelector((state: RootState) => state.admin);
    const { instance, inProgress } = useMsal();
    const { conversations, selectedId: currentConversationId } = useAppSelector(
        (state: RootState) => state.conversations,
    );
    const { activeUserInfo, features } = useAppSelector((state: RootState) => state.app);
    const { plugins } = useAppSelector((state: RootState) => state.plugins);

    const botService = new ChatArchiveService();
    const chatService = new ChatService();
    const documentImportService = new DocumentImportService();

    const userId = activeUserInfo?.id ?? '';
    const fullName = activeUserInfo?.username ?? '';
    const emailAddress = activeUserInfo?.email ?? '';
    const loggedInUser: IChatUser = {
        id: userId,
        fullName,
        emailAddress,
        photo: undefined,
        online: true,
        isTyping: false,
    };

    const getChatUserById = (id: string, chatId: string, users: IChatUser[]) => {
        if (id === `${chatId}-bot` || id.toLocaleLowerCase() === 'bot') return Constants.bot.profile;
        return users.find((user) => user.id === id);
    };
    const defaultSpecializationId = specializationsState.find((a) => a.id === 'general')?.id ?? '';
    const createChat = async (specializationId = defaultSpecializationId) => {
        const chatTitle = `Q-Pilot @ ${new Date().toLocaleString()}`;
        try {
            await chatService
                .createChatAsync(chatTitle, await AuthHelper.getSKaaSAccessToken(instance, inProgress))
                .then((result: ICreateChatSessionResponse) => {
                    const newChat: ChatState = {
                        id: result.chatSession.id,
                        title: result.chatSession.title,
                        systemDescription: result.chatSession.systemDescription,
                        memoryBalance: result.chatSession.memoryBalance,
                        messages: [result.initialBotMessage],
                        enabledHostedPlugins: result.chatSession.enabledPlugins,
                        users: [loggedInUser],
                        botProfilePicture: getBotProfilePicture(specializationsState, specializationId),
                        input: '',
                        botResponseStatus: undefined,
                        userDataLoaded: false,
                        disabled: false,
                        hidden: false,
                        specializationId,
                    };

                    dispatch(addConversation(newChat));
                    return newChat.id;
                });
        } catch (e: any) {
            const errorMessage = `Unable to create new chat. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    const getResponse = async ({ messageType, value, chatId, kernelArguments, processPlan }: GetResponseOptions) => {
        /* eslint-disable
        @typescript-eslint/no-unsafe-assignment
        */
        const chatInput: IChatMessage = {
            chatId: chatId,
            timestamp: new Date().getTime(),
            userId: activeUserInfo?.id as string,
            userName: activeUserInfo?.username as string,
            content: value,
            type: messageType,
            authorRole: AuthorRoles.User,
        };

        dispatch(addMessageToConversationFromUser({ message: chatInput, chatId: chatId }));

        const ask = {
            input: value,
            variables: [
                {
                    key: 'chatId',
                    value: chatId,
                },
                {
                    key: 'messageType',
                    value: messageType.toString(),
                },
                {
                    key: 'specialization',
                    value: conversations[chatId].specializationId
                        ? conversations[chatId].specializationId
                        : defaultSpecializationId,
                },
            ],
        };

        if (kernelArguments) {
            ask.variables.push(...kernelArguments);
        }

        try {
            const conversation = conversations[currentConversationId];
            if (!conversation.specializationId) {
                await chatService.editChatSepcializationAsync(
                    conversation.id,
                    defaultSpecializationId,
                    await AuthHelper.getSKaaSAccessToken(instance, inProgress),
                );
                dispatch(
                    editConversationSpecialization({ id: conversation.id, specializationId: defaultSpecializationId }),
                );
            }
            const askResult = await chatService
                .getBotResponseAsync(
                    ask,
                    await AuthHelper.getSKaaSAccessToken(instance, inProgress),
                    getEnabledPlugins(),
                    processPlan,
                )
                .catch((e: any) => {
                    throw e;
                });

            // Update token usage of current session
            const responseTokenUsage = askResult.variables.find((v) => v.key === 'tokenUsage')?.value;
            if (responseTokenUsage) dispatch(updateTokenUsage(JSON.parse(responseTokenUsage) as TokenUsage));
        } catch (e: any) {
            dispatch(updateBotResponseStatus({ chatId, status: undefined }));

            const errorDetails = getErrorDetails(e);
            if (errorDetails.includes('Failed to process plan')) {
                // Error should already be reflected in bot response message. Skip alert.
                return;
            }

            const action = processPlan ? 'execute plan' : 'generate bot response';
            const errorMessage = `Unable to ${action}. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    /**
     * Load all chats and messages.
     *
     * Note: This loads the chat participants and messages simultaneously.
     *
     * @async
     * @param {ISpecialization[]} specializations
     * @returns {Promise<boolean>} Indicator for chats load success
     */
    const loadChats = async (specializations: ISpecialization[]): Promise<boolean> => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            const chatSessions = await chatService.getAllChatsAsync(accessToken);

            if (chatSessions.length > 0) {
                const loadedConversations: Conversations = {};

                const participantsMessagesPromises = chatSessions.map((chatSession) =>
                    Promise.all([
                        chatService.getAllChatParticipantsAsync(chatSession.id, accessToken),
                        chatService.getChatMessagesAsync(chatSession.id, 0, 100, accessToken),
                    ]),
                );

                // Fetch all chat participants and messages at once
                const allUsersMessages = await Promise.all(participantsMessagesPromises);

                for (let i = 0; i < chatSessions.length; i++) {
                    const chatSession = chatSessions[i];
                    const [chatUsers, chatMessages] = allUsersMessages[i];

                    loadedConversations[chatSession.id] = {
                        id: chatSession.id,
                        title: chatSession.title,
                        systemDescription: chatSession.systemDescription,
                        memoryBalance: chatSession.memoryBalance,
                        users: chatUsers,
                        messages: chatMessages,
                        enabledHostedPlugins: chatSession.enabledPlugins,
                        botProfilePicture: getBotProfilePicture(specializations, chatSession.specializationId),
                        input: '',
                        botResponseStatus: undefined,
                        userDataLoaded: false,
                        disabled: false,
                        hidden: !features[FeatureKeys.MultiUserChat].enabled && chatUsers.length > 1,
                        specializationId: chatSession.specializationId,
                    };
                }

                dispatch(setConversations(loadedConversations));

                // If there are no non-hidden chats, create a new chat
                const nonHiddenChats = Object.values(loadedConversations).filter((c) => !c.hidden);
                if (nonHiddenChats.length === 0) {
                    await createChat();
                } else {
                    dispatch(setSelectedConversation(nonHiddenChats[0].id));
                }
            } else {
                // No chats exist, create first chat window
                await createChat();
            }

            return true;
        } catch (e: any) {
            const errorMessage = `Unable to load chats. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));

            return false;
        }
    };

    const downloadBot = async (chatId: string) => {
        try {
            return await botService.downloadAsync(chatId, await AuthHelper.getSKaaSAccessToken(instance, inProgress));
        } catch (e: any) {
            const errorMessage = `Unable to download the bot. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }

        return undefined;
    };

    const uploadBot = async (bot: ChatArchive) => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await botService.uploadAsync(bot, accessToken).then(async (chatSession: IChatSession) => {
                const chatMessages = await chatService.getChatMessagesAsync(chatSession.id, 0, 100, accessToken);

                const newChat: ChatState = {
                    id: chatSession.id,
                    title: chatSession.title,
                    systemDescription: chatSession.systemDescription,
                    memoryBalance: chatSession.memoryBalance,
                    users: [loggedInUser],
                    messages: chatMessages,
                    enabledHostedPlugins: chatSession.enabledPlugins,
                    botProfilePicture: getBotProfilePicture(specializationsState, chatSession.specializationId),
                    input: '',
                    botResponseStatus: undefined,
                    userDataLoaded: false,
                    disabled: false,
                    hidden: false,
                    specializationId: chatSession.specializationId,
                };

                dispatch(addConversation(newChat));
            });
        } catch (e: any) {
            const errorMessage = `Unable to upload the bot. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    /**
     * Get bot profile picture from specialization key or fall back to a standard icon.
     *
     * @param {ISpecialization[]} specializations - List of system Specializations
     * @param {string} specializationId - Unique identifier of the Specialization
     * @returns {string} Icon image path
     */
    const getBotProfilePicture = (specializations: ISpecialization[], specializationId?: string): string => {
        const specializationMatch = specializations.find((spec) => spec.id === specializationId);

        if (specializationMatch?.iconFilePath) {
            return specializationMatch.iconFilePath;
        }
        return botIcon1;
    };

    const getChatMemorySources = async (chatId: string) => {
        try {
            return await chatService.getChatMemorySourcesAsync(
                chatId,
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            );
        } catch (e: any) {
            const errorMessage = `Unable to get chat files. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }

        return [];
    };

    const getSemanticMemories = async (chatId: string, memoryName: string) => {
        try {
            return await chatService.getSemanticMemoriesAsync(
                chatId,
                memoryName,
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            );
        } catch (e: any) {
            const errorMessage = `Unable to get semantic memories. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }

        return [];
    };

    const importDocument = async (chatId: string, files: File[], uploadToGlobal: boolean) => {
        try {
            await documentImportService.importDocumentAsync(
                chatId,
                files,
                features[FeatureKeys.AzureContentSafety].enabled,
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
                uploadToGlobal,
            );
        } catch (e: any) {
            let errorDetails = getErrorDetails(e);

            // Disable Content Safety if request was unauthorized
            const contentSafetyDisabledRegEx = /Access denied: \[Content Safety] Failed to analyze image./g;
            if (contentSafetyDisabledRegEx.test(errorDetails)) {
                if (features[FeatureKeys.AzureContentSafety].enabled) {
                    errorDetails =
                        'Unable to analyze image. Content Safety is currently disabled or unauthorized service-side. Please contact your admin to enable.';
                }

                dispatch(
                    toggleFeatureState({ feature: FeatureKeys.AzureContentSafety, deactivate: true, enable: false }),
                );
            }

            const errorMessage = `Failed to upload document(s). Details: ${errorDetails}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    /*
     * Once enabled, each plugin will have a custom dedicated header in every Semantic Kernel request
     * containing respective auth information (i.e., token, encoded client info, etc.)
     * that the server can use to authenticate to the downstream APIs
     */
    const getEnabledPlugins = () => {
        return Object.values<Plugin>(plugins).filter((plugin) => plugin.enabled);
    };

    const joinChat = async (chatId: string) => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await chatService.joinChatAsync(chatId, accessToken).then(async (result: IChatSession) => {
                // Get chat messages
                const chatMessages = await chatService.getChatMessagesAsync(result.id, 0, 100, accessToken);

                // Get chat users
                const chatUsers = await chatService.getAllChatParticipantsAsync(result.id, accessToken);

                const newChat: ChatState = {
                    id: result.id,
                    title: result.title,
                    systemDescription: result.systemDescription,
                    memoryBalance: result.memoryBalance,
                    messages: chatMessages,
                    enabledHostedPlugins: result.enabledPlugins,
                    users: chatUsers,
                    botProfilePicture: getBotProfilePicture(specializationsState, result.specializationId),
                    input: '',
                    botResponseStatus: undefined,
                    userDataLoaded: false,
                    disabled: false,
                    hidden: false,
                    specializationId: result.specializationId,
                };

                dispatch(addConversation(newChat));
            });
        } catch (e: any) {
            const errorMessage = `Error joining chat ${chatId}. Details: ${getErrorDetails(e)}`;
            return { success: false, message: errorMessage };
        }

        return { success: true, message: '' };
    };

    const editChat = async (chatId: string, title: string, syetemDescription: string, memoryBalance: number) => {
        try {
            await chatService.editChatAsync(
                chatId,
                title,
                syetemDescription,
                memoryBalance,
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            );
        } catch (e: any) {
            const errorMessage = `Error editing chat ${chatId}. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    const editChatSpecialization = async (chatId: string, specializationId: string) => {
        try {
            await chatService.editChatSepcializationAsync(
                chatId,
                specializationId,
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            );
        } catch (e: any) {
            const errorMessage = `Error editing chat ${chatId}. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    const getServiceInfo = async () => {
        try {
            return await chatService.getServiceInfoAsync(await AuthHelper.getSKaaSAccessToken(instance, inProgress));
        } catch (e: any) {
            const errorMessage = `Error getting service options. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));

            return undefined;
        }
    };

    const deleteChat = async (chatId: string) => {
        const friendlyChatName = getFriendlyChatName(conversations[chatId]);
        await chatService
            .deleteChatAsync(chatId, await AuthHelper.getSKaaSAccessToken(instance, inProgress))
            .then(() => {
                dispatch(deleteConversation(chatId));

                if (Object.values(conversations).filter((c) => !c.hidden && c.id !== chatId).length === 0) {
                    // If there are no non-hidden chats, create a new chat
                    //void createChat();
                }
            })
            .catch((e: any) => {
                const errorDetails = (e as Error).message.includes('Failed to delete resources for chat id')
                    ? "Some or all resources associated with this chat couldn't be deleted. Please try again."
                    : `Details: ${(e as Error).message}`;
                dispatch(
                    addAlert({
                        message: `Unable to delete chat {${friendlyChatName}}. ${errorDetails}`,
                        type: AlertType.Error,
                        onRetry: () => void deleteChat(chatId),
                    }),
                );
            });
    };

    const processPlan = async (chatId: string, planState: PlanState, serializedPlan: string, planGoal?: string) => {
        const kernelArguments: ContextVariable[] = [
            {
                key: 'proposedPlan',
                value: serializedPlan,
            },
        ];

        let message = 'Run plan' + (planGoal ? ` with goal of: ${planGoal}` : '');
        switch (planState) {
            case PlanState.Rejected:
                message = 'No, cancel';
                break;
            case PlanState.Approved:
                message = 'Yes, proceed';
                break;
        }

        // Send plan back for processing or execution
        await getResponse({
            value: message,
            kernelArguments,
            messageType: ChatMessageType.Message,
            chatId: chatId,
            processPlan: true,
        });
    };

    return {
        getChatUserById,
        createChat,
        loadChats,
        getResponse,
        downloadBot,
        uploadBot,
        getChatMemorySources,
        getSemanticMemories,
        importDocument,
        joinChat,
        editChat,
        editChatSpecialization,
        getServiceInfo,
        deleteChat,
        processPlan,
    };
};

export function getFriendlyChatName(convo: ChatState): string {
    const messages = convo.messages;

    // Regex to match the Q-Pilot timestamp format that is used as the default chat name.
    // The format is: 'Q-Pilot @ MM/DD/YYYY, hh:mm:ss AM/PM'.
    const autoGeneratedTitleRegex =
        /Q-Pilot @ [0-9]{1,2}\/[0-9]{1,2}\/[0-9]{1,4}, [0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2} [A,P]M/;
    const firstUserMessage = messages.find(
        (message) => message.authorRole !== AuthorRoles.Bot && message.type === ChatMessageType.Message,
    );

    // If the chat title is the default Q-Pilot timestamp, use the first user message as the title.
    // If no user messages exist, use 'New Chat' as the title.
    const friendlyTitle = autoGeneratedTitleRegex.test(convo.title)
        ? (firstUserMessage?.content ?? 'New Chat')
        : convo.title;

    // Truncate the title if it is too long
    return friendlyTitle.length > 60 ? friendlyTitle.substring(0, 60) + '...' : friendlyTitle;
}
