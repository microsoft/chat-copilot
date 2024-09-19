import { useMsal } from '@azure/msal-react';
import { useAppDispatch } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';

import { getErrorDetails } from '../../components/utils/TextUtils';
import {
    addSpecialization,
    editSpecialization,
    removeSpecialization,
    setSpecializationIndexes,
    setChatCompletionDeployments,
    setSpecializations,
} from '../../redux/features/admin/adminSlice';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { ISpecialization, ISpecializationRequest } from '../models/Specialization';
import { SpecializationService } from '../services/SpecializationService';

export const useSpecialization = () => {
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();
    const specializationService = new SpecializationService();

    const loadSpecializations = async (): Promise<ISpecialization[] | undefined> => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            const specializations = await specializationService.getAllSpecializationsAsync(accessToken);
            dispatch(setSpecializations(specializations));
            return specializations;
        } catch (e: any) {
            const errorMessage = `Unable to load specializations. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            return undefined;
        }
    };

    const loadSpecializationIndexes = async () => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await specializationService.getAllSpecializationIndexesAsync(accessToken).then((result: string[]) => {
                dispatch(setSpecializationIndexes(result));
            });
        } catch (e: any) {
            const errorMessage = `Unable to load chats. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            return undefined;
        }
    };

    const loadChatCompletionDeployments = async () => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await specializationService.getAllChatCompletionDeploymentsAsync(accessToken).then((result: string[]) => {
                dispatch(setChatCompletionDeployments(result));
            });
        } catch (e: any) {
            const errorMessage = `Unable to load chat completion deployments. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            return undefined;
        }
    };

    const createSpecialization = async (data: ISpecializationRequest) => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await specializationService.createSpecializationAsync(data, accessToken).then((result: ISpecialization) => {
                dispatch(addSpecialization(result));
            });
        } catch (e: any) {
            const errorMessage = `Unable to load chats. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    const updateSpecialization = async (id: string, data: ISpecializationRequest) => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await specializationService
                .updateSpecializationAsync(id, data, accessToken)
                .then((result: ISpecialization) => {
                    dispatch(editSpecialization(result));
                });
        } catch (e: any) {
            const errorMessage = `Unable to load chats. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    const toggleSpecialization = async (id: string, isActive: boolean) => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            await specializationService
                .onOffSpecializationAsync(id, isActive, accessToken)
                .then((result: ISpecialization) => {
                    dispatch(editSpecialization(result));
                });
        } catch (e: any) {
            const errorMessage = `Unable to load chats. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        }
    };

    const deleteSpecialization = async (specializationId: string) => {
        await specializationService
            .deleteSpecializationAsync(specializationId, await AuthHelper.getSKaaSAccessToken(instance, inProgress))
            .then(() => {
                dispatch(removeSpecialization(specializationId));
            })
            .catch((e: any) => {
                const errorDetails = (e as Error).message.includes('Failed to delete resources for chat id')
                    ? "Some or all resources associated with this chat couldn't be deleted. Please try again."
                    : `Details: ${(e as Error).message}`;
                dispatch(
                    addAlert({
                        message: `Unable to delete chat {${name}}. ${errorDetails}`,
                        type: AlertType.Error,
                        onRetry: () => void deleteSpecialization(specializationId),
                    }),
                );
            });
    };

    return {
        loadSpecializations,
        loadSpecializationIndexes,
        loadChatCompletionDeployments,
        createSpecialization,
        updateSpecialization,
        toggleSpecialization,
        deleteSpecialization,
    };
};
