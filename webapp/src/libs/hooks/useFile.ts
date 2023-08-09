// Copyright (c) Microsoft. All rights reserved.

import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { FeatureKeys } from '../../redux/features/app/AppState';
import { addAlert } from '../../redux/features/app/appSlice';
import { setImportingDocumentsToConversation } from '../../redux/features/conversations/conversationsSlice';
import { AlertType } from '../models/AlertType';
import { useChat } from './useChat';
import { useContentModerator } from './useContentModerator';

export const useFile = () => {
    const { features } = useAppSelector((state: RootState) => state.app);
    const dispatch = useAppDispatch();
    const contentModerator = useContentModerator();

    const chat = useChat();

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

    function loadImage(file: File, loadCallBack: (base64Image: string) => Promise<void>): Promise<string> {
        return new Promise((resolve, reject) => {
            const fileReader = new FileReader();
            fileReader.onload = async (event: ProgressEvent<FileReader>) => {
                const content = event.target?.result as string;
                try {
                    await loadCallBack(content);
                    resolve(content);
                } catch (e) {
                    reject(e);
                }
            };
            fileReader.onerror = reject;
            fileReader.readAsDataURL(file);
        });
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
            const filesToUploadArray: File[] = [];
            const imageErrors: string[] = [];

            dispatch(
                setImportingDocumentsToConversation({
                    importingDocuments: filesArray.map((file) => file.name),
                    chatId,
                }),
            );

            for (const file of filesArray) {
                if (features[FeatureKeys.AzureContentSafety].enabled && file.type.startsWith('image/')) {
                    try {
                        await handleImageUpload(file);
                        filesToUploadArray.push(file);
                    } catch (e: any) {
                        imageErrors.push((e as Error).message);
                    }
                } else {
                    filesToUploadArray.push(file);
                }
            }

            if (imageErrors.length > 0) {
                const errorMessage = `Failed to upload image(s): ${imageErrors.join('; ')}`;
                dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            }

            if (filesToUploadArray.length > 0) {
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

    const handleImageUpload = async (file: File) => {
        await loadImage(file, contentModerator.analyzeImage).catch((error: Error) => {
            throw new Error(`'${file.name}' (${error.message})`);
        });
    };

    return {
        loadFile,
        downloadFile,
        loadImage,
        handleImport,
    };
};
