export interface UserSettingsResponse {
    settings: IUserSettings;
    adminGroupId: string;
}

export interface IUserSettings {
    darkMode: boolean;
    pluginsPersonas: boolean;
    simplifiedChat: boolean;
}
