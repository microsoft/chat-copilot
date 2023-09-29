// Copyright (c) Microsoft. All rights reserved.

import { IChatMessage } from '../models/ChatMessage';
import { BaseService } from './BaseService';
import { ServiceOptions } from '../models/ServiceOptions';

export class DocumentImportService extends BaseService {
    public importDocumentAsync = async (
        chatId: string,
        documents: File[],
        useContentSafety: boolean,
        accessToken: string,
    ) => {
        const formData = new FormData();
        formData.append('useContentSafety', useContentSafety.toString());
        for (const document of documents) {
            formData.append('formFiles', document);
        }

        return await this.getResponseAsync<IChatMessage>(
            {
                commandPath: `chats/${chatId}/documents`,
                method: 'POST',
                body: formData,
            },
            accessToken,
        );
    };

    public getContentSafetyStatusAsync = async (accessToken: string): Promise<boolean> => {
        const serviceOptions = await this.getResponseAsync<ServiceOptions>(
            {
                commandPath: 'contentSafety/status',
                method: 'GET',
            },
            accessToken,
        );

        return serviceOptions.isContentSafetyEnabled;
    };
}
