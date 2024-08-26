import { IUserSettings, UserSettingsResponse } from '../models/UserSettings';
import { BaseService } from './BaseService';

/**
 * SettingService to hold the http methods related to settings
 */
export class SettingService extends BaseService {
    /**
     * Gets the users settings
     * @param accessToken the accessToken
     * @returns
     */
    public getUserSettingsAsync = async (accessToken: string): Promise<UserSettingsResponse> => {
        return await this.getResponseAsync<UserSettingsResponse>(
            {
                commandPath: 'user-settings',
                method: 'GET',
            },
            accessToken,
        );
    };

    /**
     * Saves the users settings
     * @param accessToken the access token
     * @param body contains the setting key to update and a boolean to indicate active or inactive
     * @returns the updated settings
     */
    public saveUserSettings = async (
        accessToken: string,
        body: { setting: string; enabled: boolean },
    ): Promise<IUserSettings> => {
        return await this.getResponseAsync<IUserSettings>(
            {
                commandPath: 'user-settings',
                method: 'POST',
                body,
            },
            accessToken,
        );
    };
}
