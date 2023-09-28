// Copyright (c) Microsoft. All rights reserved.

import { useMsal } from '@azure/msal-react';
import { useAppDispatch } from '../../redux/app/hooks';
import { FeatureKeys } from '../../redux/features/app/AppState';
import { toggleFeatureState } from '../../redux/features/app/appSlice';
import { setImportingDocumentsToConversation } from '../../redux/features/conversations/conversationsSlice';
import { AuthHelper } from '../auth/AuthHelper';
import { DocumentImportService } from '../services/DocumentImportService';
import { useChat } from './useChat';

export const useFile = () => {
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();

    const chat = useChat();
    const documentImportService = new DocumentImportService();

    async function loadFile<T>(file: File, loadCallBack: (data: T) => Promise<void>): Promise<T> {
        return await new Promise((resolve, reject) => {
            const fileReader = new FileReader();
            fileReader.onload = async (event: ProgressEvent<FileReader>) => {
                const content = event.target?.result as string;
                try {
                    const parsedData = JSON.parse(content) as T;
                    await loadCallBack(parsedData);
                    resolve(parsedData);
                } catch (e) {
                    reject(e);
                }
            };
            fileReader.onerror = reject;
            fileReader.readAsText(file);
        });
    }

    function downloadFile(filename: string, content: string, type: string) {
        const data: BlobPart[] = [content];
        let file: File | null = new File(data, filename, { type });

        const link = document.createElement('a');
        link.href = URL.createObjectURL(file);
        link.download = filename;

        link.click();
        URL.revokeObjectURL(link.href);
        link.remove();
        file = null;
    }

    const handleImport = async (
        chatId: string,
        documentFileRef: React.MutableRefObject<HTMLInputElement | null>,
        file?: File,
        dragAndDropFiles?: FileList,
    ) => {
        const files = dragAndDropFiles ?? documentFileRef.current?.files;
        if (file ?? (files && files.length > 0)) {
            // Deep copy the FileList into an array so that the function
            // maintains a list of files to import before the import is complete.
            const filesArray = file ? [file] : files ? Array.from(files) : [];
            dispatch(
                setImportingDocumentsToConversation({
                    importingDocuments: filesArray.map((file) => file.name),
                    chatId,
                }),
            );

            if (filesArray.length > 0) {
                await chat.importDocument(chatId, filesArray);
            }

            dispatch(
                setImportingDocumentsToConversation({
                    importingDocuments: [],
                    chatId,
                }),
            );
        }

        // Reset the file input so that the onChange event will
        // be triggered even if the same file is selected again.
        if (documentFileRef.current?.value) {
            documentFileRef.current.value = '';
        }
    };

    const getContentSafetyStatus = async () => {
        try {
            const result = await documentImportService.getContentSafetyStatusAsync(
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            );

            if (result) {
                dispatch(
                    toggleFeatureState({ feature: FeatureKeys.AzureContentSafety, deactivate: false, enable: true }),
                );
            }
        } catch (error) {
            /* Do nothing, leave feature disabled */
        }
    };

    return {
        loadFile,
        downloadFile,
        handleImport,
        getContentSafetyStatus,
    };
};
