import { useMsal } from '@azure/msal-react';
import { useAppDispatch } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';

import { getErrorDetails } from '../../components/utils/TextUtils';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { IUserSettings } from '../models/UserSettings';
import { SettingService } from '../services/SettingsService';

/**
 * Hook that provides access to settings related functions
 * @returns functions to get and update settings
 */
export const useSettings = () => {
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();
    const settingService = new SettingService();

    /**
     * Loads the users settings from the backend
     * @returns the users settings
     */
    const getSettings = async () => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            return settingService.getUserSettingsAsync(accessToken);
        } catch (e: any) {
            const errorMessage = `Unable to load user settings. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            return undefined;
        }
    };

    /**
     * Updates a setting to the specified boolean
     * @param setting the key to toggle
     * @param enabled the value to toggle to
     * @returns the updated settings
     */
    const updateSetting = async (setting: keyof IUserSettings, enabled: boolean) => {
        try {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            return settingService.saveUserSettings(accessToken, {
                setting,
                enabled,
            });
        } catch (e: any) {
            const errorMessage = `Unable to load user settings. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
            return undefined;
        }
    };

    return {
        getSettings,
        updateSetting,
    };
};
