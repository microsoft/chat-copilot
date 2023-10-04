// Copyright (c) Microsoft. All rights reserved.

import { Plugin } from '../../redux/features/plugins/PluginsState';
import { ChatMemorySource } from '../models/ChatMemorySource';
import { IChatMessage } from '../models/ChatMessage';
import { IChatParticipant } from '../models/ChatParticipant';
import { IChatSession, ICreateChatSessionResponse } from '../models/ChatSession';
import { IChatUser } from '../models/ChatUser';
import { ServiceInfo } from '../models/ServiceInfo';
import { IAsk, IAskVariables } from '../semantic-kernel/model/Ask';
import { IAskResult } from '../semantic-kernel/model/AskResult';
import { ICustomPlugin } from '../semantic-kernel/model/CustomPlugin';
import { BaseService } from './BaseService';

export class ChatService extends BaseService {
    public createChatAsync = async (title: string, accessToken: string): Promise<ICreateChatSessionResponse> => {
        const body = {
            title,
        };

        const result = await this.getResponseAsync<ICreateChatSessionResponse>(
            {
                commandPath: 'chats',
                method: 'POST',
                body,
            },
            accessToken,
        );

        return result;
    };

    public getChatAsync = async (chatId: string, accessToken: string): Promise<IChatSession> => {
        const result = await this.getResponseAsync<IChatSession>(
            {
                commandPath: `chats/${chatId}`,
                method: 'GET',
            },
            accessToken,
        );

        return result;
    };

    public getAllChatsAsync = async (accessToken: string): Promise<IChatSession[]> => {
        const result = await this.getResponseAsync<IChatSession[]>(
            {
                commandPath: 'chats',
                method: 'GET',
            },
            accessToken,
        );
        return result;
    };

    public getChatMessagesAsync = async (
        chatId: string,
        startIdx: number,
        count: number,
        accessToken: string,
    ): Promise<IChatMessage[]> => {
        const result = await this.getResponseAsync<IChatMessage[]>(
            {
                commandPath: `chats/${chatId}/messages?startIdx=${startIdx}&count=${count}`,
                method: 'GET',
            },
            accessToken,
        );

        // Messages are returned with most recent message at index 0 and oldest message at the last index,
        // so we need to reverse the order for render
        return result.reverse();
    };

    public editChatAsync = async (
        chatId: string,
        title: string,
        systemDescription: string,
        memoryBalance: number,
        accessToken: string,
    ): Promise<any> => {
        const body: IChatSession = {
            id: chatId,
            title,
            systemDescription,
            memoryBalance,
            enabledPlugins: [], // edit will not modify the enabled plugins
        };

        const result = await this.getResponseAsync<IChatSession>(
            {
                commandPath: `chats/${chatId}`,
                method: 'PATCH',
                body,
            },
            accessToken,
        );

        return result;
    };

    public deleteChatAsync = async (chatId: string, accessToken: string): Promise<object> => {
        const result = await this.getResponseAsync<object>(
            {
                commandPath: `chats/${chatId}`,
                method: 'DELETE',
            },
            accessToken,
        );

        return result;
    };

    public getBotResponseAsync = async (
        ask: IAsk,
        accessToken: string,
        enabledPlugins?: Plugin[],
        processPlan = false,
    ): Promise<IAskResult> => {
        // If skill requires any additional api properties, append to context
        if (enabledPlugins && enabledPlugins.length > 0) {
            const openApiSkillVariables: IAskVariables[] = [];

            // List of custom plugins to append to context variables
            const customPlugins: ICustomPlugin[] = [];

            for (const plugin of enabledPlugins) {
                // If user imported a manifest domain, add custom plugin
                if (plugin.manifestDomain) {
                    customPlugins.push({
                        nameForHuman: plugin.name,
                        nameForModel: plugin.nameForModel as string,
                        authHeaderTag: plugin.headerTag,
                        authType: plugin.authRequirements.personalAccessToken ? 'user_http' : 'none',
                        manifestDomain: plugin.manifestDomain,
                    });
                }

                // If skill requires any additional api properties, append to context variables
                if (plugin.apiProperties) {
                    const apiProperties = plugin.apiProperties;

                    for (const property in apiProperties) {
                        const propertyDetails = apiProperties[property];

                        if (propertyDetails.required && !propertyDetails.value) {
                            throw new Error(`Missing required property ${property} for ${plugin.name} skill.`);
                        }

                        if (propertyDetails.value) {
                            openApiSkillVariables.push({
                                key: `${property}`,
                                value: propertyDetails.value,
                            });
                        }
                    }
                }
            }

            if (customPlugins.length > 0) {
                openApiSkillVariables.push({
                    key: `customPlugins`,
                    value: JSON.stringify(customPlugins),
                });
            }

            ask.variables = ask.variables ? ask.variables.concat(openApiSkillVariables) : openApiSkillVariables;
        }

        const chatId = ask.variables?.find((variable) => variable.key === 'chatId')?.value as string;

        const result = await this.getResponseAsync<IAskResult>(
            {
                commandPath: `chats/${chatId}/${processPlan ? 'processPlan' : 'messages'}`,
                method: 'POST',
                body: ask,
            },
            accessToken,
            enabledPlugins,
        );

        return result;
    };

    public joinChatAsync = async (chatId: string, accessToken: string): Promise<IChatSession> => {
        await this.getResponseAsync<any>(
            {
                commandPath: `chats/${chatId}/participants`,
                method: 'POST',
            },
            accessToken,
        );

        return await this.getChatAsync(chatId, accessToken);
    };

    public getChatMemorySourcesAsync = async (chatId: string, accessToken: string): Promise<ChatMemorySource[]> => {
        const result = await this.getResponseAsync<ChatMemorySource[]>(
            {
                commandPath: `chats/${chatId}/documents`,
                method: 'GET',
            },
            accessToken,
        );

        return result;
    };

    public getAllChatParticipantsAsync = async (chatId: string, accessToken: string): Promise<IChatUser[]> => {
        const result = await this.getResponseAsync<IChatParticipant[]>(
            {
                commandPath: `chats/${chatId}/participants`,
                method: 'GET',
            },
            accessToken,
        );

        const chatUsers = result.map<IChatUser>((participant) => ({
            id: participant.userId,
            online: false,
            fullName: '', // The user's full name is not returned from the server
            emailAddress: '', // The user's email address is not returned from the server
            isTyping: false,
            photo: '',
        }));

        return chatUsers;
    };

    public getSemanticMemoriesAsync = async (
        chatId: string,
        memoryName: string,
        accessToken: string,
    ): Promise<string[]> => {
        const result = await this.getResponseAsync<string[]>(
            {
                commandPath: `chats/${chatId}/memories?type=${memoryName}`,
                method: 'GET',
            },
            accessToken,
        );

        return result;
    };

    public getServiceInfoAsync = async (accessToken: string): Promise<ServiceInfo> => {
        const result = await this.getResponseAsync<ServiceInfo>(
            {
                commandPath: `info`,
                method: 'GET',
            },
            accessToken,
        );

        return result;
    };
}
