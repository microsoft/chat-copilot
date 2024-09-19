// Copyright (c) Microsoft. All rights reserved.

import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { FluentProvider, makeStyles, shorthands, tokens } from '@fluentui/react-components';

import {
    AccountInfo,
    AuthenticationResult,
    InteractionRequiredAuthError,
    IPublicClientApplication,
    PopupRequest,
} from '@azure/msal-browser';
import { Client, ResponseType } from '@microsoft/microsoft-graph-client';
import { jwtDecode } from 'jwt-decode';
import * as React from 'react';
import { useEffect } from 'react';
import Chat from './components/chat/Chat';
import Header from './components/header/Header';
import { Loading, Login } from './components/views';
import { AuthHelper } from './libs/auth/AuthHelper';
import { useChat, useFile, useSpecialization } from './libs/hooks';
import { useSettings } from './libs/hooks/useSettings';
import { useAppDispatch, useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { FeatureKeys } from './redux/features/app/AppState';
import { setFeatureFlag, setServiceInfo, updateActiveUserInfo } from './redux/features/app/appSlice';
import { semanticKernelDarkTheme, semanticKernelLightTheme } from './styles';

/**
 * Changes to support specialization
 */
export const useClasses = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        height: '100vh',
        width: '100%',
        ...shorthands.overflow('hidden'),
    },
    header: {
        alignItems: 'center',
        backgroundColor: tokens.colorBrandForeground2,
        color: tokens.colorNeutralForegroundOnBrand,
        display: 'flex',
        '& h1': {
            paddingLeft: tokens.spacingHorizontalXL,
            display: 'flex',
        },
        height: '48px',
        justifyContent: 'space-between',
        width: '100%',
    },
    persona: {
        marginRight: tokens.spacingHorizontalXXL,
    },
    cornerItems: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingHorizontalS),
    },
});

export enum AppState {
    ProbeForBackend,
    SettingUserInfo,
    ErrorLoadingChats,
    ErrorLoadingUserInfo,
    LoadingChats,
    Chat,
    SigningOut,
}

interface JWTPayload {
    groups: string[];
}

const App = () => {
    const classes = useClasses();

    const [appState, setAppState] = React.useState(AppState.ProbeForBackend);
    const dispatch = useAppDispatch();

    const { instance, inProgress } = useMsal();
    const { features, isMaintenance, activeUserInfo } = useAppSelector((state: RootState) => state.app);
    const isAuthenticated = useIsAuthenticated();

    const chat = useChat();
    const file = useFile();
    const specialization = useSpecialization();
    const settings = useSettings();

    const getUserImage = async (accessToken: string, _id: string) => {
        try {
            const client = getAuthenticatedClient(accessToken);
            const response = (await client.api('/me/photo/$value').responseType(ResponseType.RAW).get()) as Response;
            return await blobToBase64(await response.blob());
        } catch (e) {
            return;
        }
    };

    const getAuthenticatedClient = (accessToken: string) => {
        return Client.init({
            authProvider: (done: (any: any, accessToken: string) => void) => {
                done(null, accessToken);
            },
        });
    };

    const blobToBase64 = async (blob: Blob): Promise<string> => {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onerror = reject;
            reader.onload = (_) => {
                resolve(reader.result as string);
            };
            reader.readAsDataURL(blob);
        });
    };

    const acquireTokenRequest = async (
        instance: IPublicClientApplication,
        account: AccountInfo | null,
        tokenRequest: PopupRequest,
    ) => {
        if (account) {
            return instance
                .acquireTokenSilent({
                    ...tokenRequest,
                    account: account,
                })
                .catch((error) => {
                    if (error instanceof InteractionRequiredAuthError) {
                        return instance.acquireTokenPopup(tokenRequest);
                    }
                    throw error;
                });
        }
        return null;
    };

    function loadUser(instance: IPublicClientApplication, account: AccountInfo) {
        acquireTokenRequest(instance, account, {
            scopes: ['User.Read'],
        })
            .then((result: AuthenticationResult | null) => {
                if (result) {
                    getUserImage(result.accessToken, account.username)
                        .then((image) => {
                            dispatch(
                                updateActiveUserInfo({
                                    id: `${account.localAccountId}.${account.tenantId}`,
                                    email: account.username, // username is the email address
                                    username: account.name ?? account.username,
                                    image: image,
                                    id_token: account.idToken ?? '',
                                }),
                            );
                        })
                        .catch((e) => {
                            console.error(e);
                        });
                }
            })
            .catch((e) => {
                console.error(e);
            });
    }

    /**
     * Load chats and set dependant state.
     *
     * Note: This prevents the race condition with chats and specializations.
     * Chats need specializations to be loaded beforehand to use chat icons / images.
     *
     * @async
     * @returns {Promise<Promise<void>>}
     */
    const loadAppStateAsync = async (): Promise<void> => {
        try {
            const [loadedSpecializations] = await Promise.all([
                specialization.loadSpecializations(),
                specialization.loadSpecializationIndexes(),
                specialization.loadChatCompletionDeployments(),
            ]);

            const [serviceInfo] = await Promise.all([
                chat.getServiceInfo(),
                file.getContentSafetyStatus(),
                chat.loadChats(loadedSpecializations ?? []),
            ]);

            if (serviceInfo) {
                dispatch(setServiceInfo(serviceInfo));
            }

            await loadSettings();

            setAppState(AppState.Chat);
        } catch (err) {
            setAppState(AppState.ErrorLoadingChats);
        }
    };

    /**
     * Loads the users settings
     *
     * @async
     */
    async function loadSettings() {
        const loadedSettings = await settings.getSettings();
        if (loadedSettings) {
            dispatch(
                updateActiveUserInfo({
                    hasAdmin: activeUserInfo?.groups.includes(loadedSettings.adminGroupId) ?? false,
                }),
            );
            dispatch(
                setFeatureFlag({
                    key: FeatureKeys.DarkMode,
                    enabled: loadedSettings.settings.darkMode,
                }),
            );
            dispatch(
                setFeatureFlag({
                    enabled: loadedSettings.settings.pluginsPersonas,
                    key: FeatureKeys.PluginsPlannersAndPersonas,
                }),
            );
            dispatch(
                setFeatureFlag({
                    key: FeatureKeys.SimplifiedExperience,
                    enabled: loadedSettings.settings.simplifiedChat,
                }),
            );
        }
    }

    useEffect(() => {
        if (isMaintenance && appState !== AppState.ProbeForBackend) {
            setAppState(AppState.ProbeForBackend);
            return;
        }

        if (isAuthenticated && appState === AppState.SettingUserInfo) {
            const account = instance.getActiveAccount();
            if (!account) {
                setAppState(AppState.ErrorLoadingUserInfo);
            } else {
                const decoded: JWTPayload = jwtDecode(account.idToken ?? '');
                dispatch(
                    updateActiveUserInfo({
                        groups: decoded.groups,
                    }),
                );

                loadUser(instance, account);
                setAppState(AppState.LoadingChats);
            }
        }

        if ((isAuthenticated || !AuthHelper.isAuthAAD()) && appState === AppState.LoadingChats) {
            void loadAppStateAsync();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [instance, inProgress, isAuthenticated, appState, isMaintenance]);

    const content = <Chat classes={classes} appState={appState} setAppState={setAppState} />;
    return (
        <FluentProvider
            className="app-container"
            theme={features[FeatureKeys.DarkMode].enabled ? semanticKernelDarkTheme : semanticKernelLightTheme}
        >
            {AuthHelper.isAuthAAD() ? (
                <>
                    <UnauthenticatedTemplate>
                        <div className={classes.container}>
                            <Header appState={appState} setAppState={setAppState} showPluginsAndSettings={false} />
                            {appState === AppState.SigningOut && <Loading text="Signing you out..." />}
                            {appState !== AppState.SigningOut && <Login />}
                        </div>
                    </UnauthenticatedTemplate>
                    <AuthenticatedTemplate>{content}</AuthenticatedTemplate>
                </>
            ) : (
                content
            )}
        </FluentProvider>
    );
};

export default App;
