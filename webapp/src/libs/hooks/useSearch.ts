// Copyright (c) Microsoft. All rights reserved.

import { useMsal } from '@azure/msal-react';
import { useAppDispatch } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { ChatMessageType } from '../models/ChatMessage';
import { IAskVariables } from '../semantic-kernel/model/Ask';
import { getErrorDetails } from '../../components/utils/TextUtils';
import { SearchService } from '../services/SearchService';
import { setSearch } from '../../redux/features/search/searchSlice';
import { searchData } from '../../assets/const';

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
    
    // const userId = activeUserInfo?.id ?? '';
    // const fullName = activeUserInfo?.username ?? '';
    // const emailAddress = activeUserInfo?.email ?? '';
    // const loggedInUser: IChatUser = {
    //     id: userId,
    //     fullName,
    //     emailAddress,
    //     photo: undefined, // TODO: [Issue #45] Make call to Graph /me endpoint to load photo
    //     online: true,
    //     isTyping: false,
    // };

    const getResponse = ( value : string ) => {
        /* eslint-disable 
        @typescript-eslint/no-unsafe-assignment
        */
        // const ask = {
        //     input: value,
        // };

        try {
            // const searchResult = 
            // await searchService
            //     .getSearchResponseAsync(
            //         ask,
            //         await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            //     )
            //     .catch((e: any) => {
            //         throw e;
            //     });
            console.log(value)
            const searchResult = searchData
             dispatch(setSearch(searchResult))
        
        } catch (e: any) {

            const errorDetails = getErrorDetails(e);
            if (errorDetails.includes('Failed to process plan')) {
                // Error should already be reflected in bot response message. Skip alert.
                return;
            }

            const errorMessage = `Unable to execute plan. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };
    
    const getServiceInfo = async () => {
        try {
            return await searchService.getServiceInfoAsync(await AuthHelper.getSKaaSAccessToken(instance, inProgress));
        } catch (e: any) {
            const errorMessage = `Error getting service options. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));

            return undefined;
        }
    };

    

    

    return {
        getResponse,
        getServiceInfo,
    };
};
