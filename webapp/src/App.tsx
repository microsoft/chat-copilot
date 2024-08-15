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
import * as React from 'react';
import { useEffect } from 'react';
import { jwtDecode } from 'jwt-decode';
import Chat from './components/chat/Chat';
import { Loading, Login } from './components/views';
import { AuthHelper } from './libs/auth/AuthHelper';
import { useChat, useFile, useSpecialization } from './libs/hooks';
import { useAppDispatch, useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { FeatureKeys } from './redux/features/app/AppState';
import { setActiveUserInfo, setServiceInfo, setSpecialization } from './redux/features/app/appSlice';
import { semanticKernelDarkTheme, semanticKernelLightTheme } from './styles';
import Header from './components/header/Header';

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
    const { features, isMaintenance } = useAppSelector((state: RootState) => state.app);
    const isAuthenticated = useIsAuthenticated();

    const chat = useChat();
    const file = useFile();
    const specialization = useSpecialization();

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
                            const decoded: JWTPayload = jwtDecode(account.idToken ?? '');
                            dispatch(
                                setActiveUserInfo({
                                    id: `${account.localAccountId}.${account.tenantId}`,
                                    email: account.username, // username is the email address
                                    username: account.name ?? account.username,
                                    image: image,
                                    groups: decoded.groups,
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
                loadUser(instance, account);
                setAppState(AppState.LoadingChats);
            }
        }

        if ((isAuthenticated || !AuthHelper.isAuthAAD()) && appState === AppState.LoadingChats) {
            void Promise.all([
                // Load all chats from memory
                chat
                    .loadChats()
                    .then(() => {
                        setAppState(AppState.Chat);
                    })
                    .catch(() => {
                        setAppState(AppState.ErrorLoadingChats);
                    }),

                // Check if content safety is enabled
                file.getContentSafetyStatus(),

                // Load service information
                chat.getServiceInfo().then((serviceInfo) => {
                    if (serviceInfo) {
                        dispatch(setServiceInfo(serviceInfo));
                    }
                }),
                //Get all specializations
                specialization.getSpecializations().then((specializations) => {
                    if (specializations) {
                        dispatch(setSpecialization(specializations));
                    }
                }),
            ]);
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
