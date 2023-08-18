// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../models/ChatMessage';
import { BaseService } from './BaseService';

export class DocumentImportService extends BaseService {
    public importDocumentAsync = async (
        chatId: string,
        documents: File[],
        useContentSafety: boolean,
        accessToken: string,
    ) => {
        const formData = new FormData();
        formData.append('chatId', chatId);
        formData.append('documentScope', 'Chat');
        formData.append('useContentSafety', useContentSafety.toString());
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

    public getContentSafetyStatusAsync = async (accessToken: string): Promise<boolean> => {
        return await this.getResponseAsync<boolean>(
            {
                commandPath: 'contentSafety/status',
                method: 'GET',
            },
            accessToken,
        );
    };
}
