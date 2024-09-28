import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { setFeatureFlag, setServiceInfo, updateActiveUserInfo } from '../../redux/features/app/appSlice';
import { useSettings } from './useSettings';
import { FeatureKeys } from '../../redux/features/app/AppState';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { RootState } from '../../redux/app/store';
import { AppState } from '../../App';
import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import { jwtDecode } from 'jwt-decode';
import { AuthHelper } from '../auth/AuthHelper';
import { useSpecialization } from './useSpecialization';
import { useChat } from './useChat';
import { useFile } from './useFile';
import { GraphService } from '../services/GraphService';
import { UserSettingsResponse } from '../models/UserSettings';

/**
 * Hook to load the application state.
 *
 * Loads: User, Settings, Specializations, Chats, Service Info etc...
 *
 * @returns {[AppState, Dispatch<SetStateAction<AppState>>]}
 */
export const useAppLoader = (): [AppState, Dispatch<SetStateAction<AppState>>] => {
    const dispatch = useAppDispatch();

    const { instance, inProgress } = useMsal();
    const isAuthenticated = useIsAuthenticated();
    const specialization = useSpecialization();
    const settings = useSettings();
    const chat = useChat();
    const file = useFile();

    const { isMaintenance } = useAppSelector((state: RootState) => state.app);

    // The current state of the application.
    const [appState, setAppState] = useState(AppState.ProbeForBackend);

    // Constants to determine the flow of the app on load
    const shouldProbeForBackend = isMaintenance && appState !== AppState.ProbeForBackend;
    const canLoadUser = isAuthenticated && appState === AppState.SettingUserInfo;
    const canLoadChats = (isAuthenticated || !AuthHelper.isAuthAAD()) && appState === AppState.LoadingChats;

    /**
     * Set the loaded settings for the user.
     *
     * @async
     * @returns {void}
     */
    const setSettings = (userSettings?: UserSettingsResponse) => {
        if (!userSettings) {
            return;
        }

        const { darkMode, pluginsPersonas, simplifiedChat } = userSettings.settings;
        // Set the settings
        dispatch(setFeatureFlag({ key: FeatureKeys.DarkMode, enabled: darkMode }));
        dispatch(setFeatureFlag({ key: FeatureKeys.PluginsPlannersAndPersonas, enabled: pluginsPersonas }));
        dispatch(setFeatureFlag({ key: FeatureKeys.SimplifiedExperience, enabled: simplifiedChat }));
    };

    /**
     * Load the user and attempt to retrieve the microsoft user image.
     *
     * @returns {Promise<void>}
     */
    const loadUser = async () => {
        try {
            const account = instance.getActiveAccount();

            if (!account?.idToken) {
                setAppState(AppState.ErrorLoadingUserInfo);
                return;
            }

            // Get the user groups from the token
            const { groups } = jwtDecode<{ groups: string[] }>(account.idToken);

            const graphService = new GraphService();

            // Get the user avatar URI and application settings
            const [avatar, userSettings] = await Promise.all([
                graphService.getUserAvatar(instance, inProgress),
                settings.getSettings(),
            ]);

            const hasAdminRole = userSettings?.adminGroupId ? groups.includes(userSettings.adminGroupId) : false;

            // Dispatch the user information
            dispatch(
                updateActiveUserInfo({
                    id: `${account.localAccountId}.${account.tenantId}`,
                    email: account.username,
                    username: account.name ?? account.username,
                    image: avatar,
                    id_token: account.idToken ?? '',
                    groups: groups,
                    hasAdmin: hasAdminRole,
                }),
            );

            // Set the user application settings
            setSettings(userSettings);

            // Trigger the next state
            setAppState(AppState.LoadingChats);
        } catch (err) {
            setAppState(AppState.ErrorLoadingUserInfo);
        }
    };

    /**
     * Load chats and set dependant state.
     *
     * Note: This prevents the race condition with chats and specializations.
     * Chats need specializations to be loaded beforehand to use the bot icons / images.
     *
     * @async
     * @returns {Promise<Promise<void>>}
     */
    const loadChats = async () => {
        try {
            // Load as much data as possible in parallel
            const [loadedSpecializations, serviceInfo] = await Promise.all([
                specialization.loadSpecializations(),
                chat.getServiceInfo(),
                specialization.loadSpecializationIndexes(),
                specialization.loadChatCompletionDeployments(),
                file.getContentSafetyStatus(),
            ]);

            // Load the chats and inject the specializations
            await chat.loadChats(loadedSpecializations ?? []);

            if (serviceInfo) {
                dispatch(setServiceInfo(serviceInfo));
            }

            // Trigger the next state
            setAppState(AppState.Chat);
        } catch (err) {
            setAppState(AppState.ErrorLoadingChats);
        }
    };

    // Watches for changes in the application state and loads the app accordingly
    useEffect(() => {
        const loadApp = async () => {
            // If the app is in maintenance, we need to probe for the backend
            if (shouldProbeForBackend) {
                setAppState(AppState.ProbeForBackend);
                return;
            }

            // If the user is authenticated, we can load the user and application settings
            if (canLoadUser) {
                await loadUser();
                return;
            }

            // If the user is authenticated or AAD is disabled, we can load the chats
            if (canLoadChats) {
                await loadChats();
                return;
            }
        };

        void loadApp();

        // Disabling dependencies for loadUser, loadSettings, and loadChats to prevent infinite loops
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [canLoadChats, canLoadUser, shouldProbeForBackend]);

    return [appState, setAppState];
};
