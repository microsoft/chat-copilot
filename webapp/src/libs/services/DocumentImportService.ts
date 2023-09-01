// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../models/ChatMessage';
import { BaseService } from './BaseService';

export class DocumentImportService extends BaseService {
    public importDocumentAsync = async (
        userId: string,
        userName: string,
        chatId: string,
        documents: File[],
        accessToken: string,
    ) => {
        const formData = new FormData();
        formData.append('userId', userId);
        formData.append('userName', userName);
        formData.append('chatId', chatId);
        formData.append('documentScope', 'Chat');
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

    public deleteDocumentAsync = async (
        userId: string,
        chatId: string,
        documentId: string,
        accessToken: string,
    ) => {
        const formData = new FormData();
        formData.append('userId', userId);
        formData.append('chatId', chatId);
        formData.append('documentId', documentId);

        return await this.getResponseAsync<string>(
            {
                commandPath: 'deleteDocument',
                method: 'POST',
                body: formData,
            },
            accessToken,
        );
    };
}
