// Copyright (c) Microsoft. All rights reserved.

import { AuthenticatedTemplate, UnauthenticatedTemplate, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { FluentProvider, Subtitle1, makeStyles, shorthands, tokens } from '@fluentui/react-components';

import * as React from 'react';
import { useEffect } from 'react';
import { UserSettingsMenu } from './components/header/UserSettingsMenu';
import { PluginGallery } from './components/open-api-plugins/PluginGallery';
import { BackendProbe, ChatView, Error, Loading, Login } from './components/views';
import { AuthHelper } from './libs/auth/AuthHelper';
import { useChat, useFile } from './libs/hooks';
import { AlertType } from './libs/models/AlertType';
import { useAppDispatch, useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { FeatureKeys } from './redux/features/app/AppState';
import { addAlert, setActiveUserInfo, setServiceOptions } from './redux/features/app/appSlice';
import { semanticKernelDarkTheme, semanticKernelLightTheme } from './styles';

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

enum AppState {
    ProbeForBackend,
    SettingUserInfo,
    ErrorLoadingUserInfo,
    ErrorLoadingAuthInfo,
    ErrorLoadingChats,
    SiteMaintenance,
    LoadingAuthInfo,
    LoadingChats,
    Chat,
    SigningOut,
}

const App = () => {
    const classes = useClasses();

    const [appState, setAppState] = React.useState(AppState.LoadingAuthInfo);
    const dispatch = useAppDispatch();

    const { instance, inProgress } = useMsal();
    const { authConfig, features, isMaintenance } = useAppSelector((state: RootState) => state.app);
    const isAuthenticated = useIsAuthenticated();

    const chat = useChat();
    const file = useFile();

    useEffect(() => {
        // if the auth info is being loaded, change the state if:
        //      1. the `authConfig` is undefined, meaning an error occurred during load, or
        //      2. the `authConfig` has keys, meaning it was loaded successfully.
        if (appState === AppState.LoadingAuthInfo && (!authConfig || Object.keys(authConfig).length)) {
            setAppState(
                authConfig
                    ? AuthHelper.isAuthAAD()
                        ? AppState.SettingUserInfo
                        : AppState.LoadingChats
                    : authConfig === null
                    ? AppState.ProbeForBackend
                    : AppState.ErrorLoadingAuthInfo,
            );
        }
    }, [dispatch, appState, authConfig]);

    useEffect(() => {
        if (isMaintenance && appState !== AppState.SiteMaintenance) {
            setAppState(AppState.SiteMaintenance);
            return;
        }

        if (isAuthenticated && appState === AppState.SettingUserInfo) {
            const account = instance.getActiveAccount();
            if (!account) {
                setAppState(AppState.ErrorLoadingUserInfo);
            } else {
                dispatch(
                    setActiveUserInfo({
                        id: `${account.localAccountId}.${account.tenantId}`,
                        email: account.username, // username is the email address
                        username: account.name ?? account.username,
                    }),
                );

                // Privacy disclaimer for internal Microsoft users
                if (account.username.split('@')[1] === 'microsoft.com') {
                    dispatch(
                        addAlert({
                            message:
                                'By using Chat Copilot, you agree to protect sensitive data, not store it in chat, and allow chat history collection for service improvements. This tool is for internal use only.',
                            type: AlertType.Info,
                        }),
                    );
                }

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

                // Load service options
                chat.getServiceOptions().then((serviceOptions) => {
                    if (serviceOptions) {
                        dispatch(setServiceOptions(serviceOptions));
                    }
                }),
            ]);
        }

        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [instance, inProgress, isAuthenticated, appState, isMaintenance]);

    // TODO: [Issue #41] handle error case of missing account information
    return (
        <FluentProvider
            className="app-container"
            theme={features[FeatureKeys.DarkMode].enabled ? semanticKernelDarkTheme : semanticKernelLightTheme}
        >
            {AuthHelper.isAuthAAD() ? (
                <>
                    <UnauthenticatedTemplate>
                        <div className={classes.container}>
                            <div className={classes.header}>
                                <Subtitle1 as="h1">Chat Copilot</Subtitle1>
                            </div>
                            {appState === AppState.SigningOut && <Loading text="Signing you out..." />}
                            {appState !== AppState.SigningOut && <Login />}
                        </div>
                    </UnauthenticatedTemplate>
                    <AuthenticatedTemplate>
                        <Chat classes={classes} appState={appState} setAppState={setAppState} />
                    </AuthenticatedTemplate>
                </>
            ) : (
                <Chat classes={classes} appState={appState} setAppState={setAppState} />
            )}
        </FluentProvider>
    );
};

const Chat = ({
    classes,
    appState,
    setAppState,
}: {
    classes: ReturnType<typeof useClasses>;
    appState: AppState;
    setAppState: (state: AppState) => void;
}) => {
    return (
        <div className={classes.container}>
            <div className={classes.header}>
                <Subtitle1 as="h1">Chat Copilot</Subtitle1>
                {appState > AppState.LoadingAuthInfo && (
                    <div className={classes.cornerItems}>
                        <div className={classes.cornerItems}>
                            <PluginGallery />
                            <UserSettingsMenu
                                setLoadingState={() => {
                                    setAppState(AppState.SigningOut);
                                }}
                            />
                        </div>
                    </div>
                )}
            </div>
            {appState === AppState.ProbeForBackend && (
                <BackendProbe
                    onBackendFound={() => {
                        setAppState(AuthHelper.isAuthAAD() ? AppState.SettingUserInfo : AppState.LoadingChats);
                    }}
                />
            )}
            {appState === AppState.SettingUserInfo && (
                <Loading text={'Hang tight while we fetch your information...'} />
            )}
            {appState === AppState.ErrorLoadingUserInfo && (
                <Error text={'Oops, something went wrong. Please try signing out and signing back in.'} />
            )}
            {appState === AppState.ErrorLoadingAuthInfo && (
                <Error text={'Oops, unable to authentication info. Please try refreshing the page.'} />
            )}
            {appState === AppState.ErrorLoadingChats && (
                <Error text={'Oops, unable to load chats. Please try refreshing the page.'} />
            )}
            {appState === AppState.SiteMaintenance && <Loading text="Backend is currently under maintenance" />}
            {appState === AppState.LoadingAuthInfo && <Loading text="Loading authentication info..." />}
            {appState === AppState.LoadingChats && <Loading text="Loading chats..." />}
            {appState === AppState.Chat && <ChatView />}
        </div>
    );
};

export default App;
