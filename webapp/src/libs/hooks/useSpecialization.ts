import { useMsal } from '@azure/msal-react';
import { useAppDispatch } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';

import { SpecializationService } from '../services/SpecializationService';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { getErrorDetails } from '../../components/utils/TextUtils';

export const useSpecialization = () => {
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();
    const specializationService = new SpecializationService();

    const getSpecializations = async () => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            return await specializationService.getAllSpecializationsAsync(accessToken);
        } catch (e: any) {
            const errorMessage = `Unable to load chats. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            return undefined;
        }
    };

    return {
        getSpecializations,
    };
};
