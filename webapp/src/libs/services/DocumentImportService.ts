// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../models/ChatMessage';
import { BaseService } from './BaseService';

export class DocumentImportService extends BaseService {
    public importDocumentAsync = async (
        userId: string,
        userName: string,
        chatId: string,
        documents: File[],
        useContentModerator: boolean,
        accessToken: string,
    ) => {
        const formData = new FormData();
        formData.append('userId', userId);
        formData.append('userName', userName);
        formData.append('chatId', chatId);
        formData.append('documentScope', 'Chat');
        formData.append('useContentModerator', useContentModerator.toString());
        for (const document of documents) {
            formData.append('formFiles', document);
        }

        return await this.getResponseAsync<IChatMessage>(
            {
                commandPath: 'importDocuments',
                method: 'POST',
                body: formData,
            },
            accessToken,
        );
    };

    public getContentModerationStatusAsync = async (accessToken: string): Promise<boolean> => {
        return await this.getResponseAsync<boolean>(
            {
                commandPath: 'contentModerator/status',
                method: 'GET',
            },
            accessToken,
        );
    };
}
