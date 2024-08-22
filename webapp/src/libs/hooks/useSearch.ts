// Copyright (c) Microsoft. All rights reserved.

import { useMsal } from '@azure/msal-react';
import { getErrorDetails } from '../../components/utils/TextUtils';
import { useAppDispatch } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';
import { setSearch } from '../../redux/features/search/searchSlice';
import { SearchResponse } from '../../redux/features/search/SearchState';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { ChatMessageType } from '../models/ChatMessage';
import { IAskSearch, IAskVariables } from '../semantic-kernel/model/Ask';
import { SearchService } from '../services/SearchService';

export interface GetResponseOptions {
    messageType: ChatMessageType;
    value: string;
    chatId: string;
    kernelArguments?: IAskVariables[];
    processPlan?: boolean;
}

export const useSearch = () => {
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();
    const searchService = new SearchService();

    const getResponse = async (specializationId: string, value: string) => {
        const searchAsk: IAskSearch = {
            specializationId,
            search: value,
        };
        try {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-call
            await searchService
                .getSearchResponseAsync(searchAsk, await AuthHelper.getSKaaSAccessToken(instance, inProgress))
                .then((searchResult: SearchResponse) => {
                    dispatch(setSearch(searchResult));
                });
        } catch (e: any) {
            const errorMessage = `Unable to search. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };
    return {
        getResponse,
    };
};
