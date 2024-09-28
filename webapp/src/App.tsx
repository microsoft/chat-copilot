// Copyright (c) Microsoft. All rights reserved.

import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { FluentProvider, makeStyles, shorthands, tokens } from '@fluentui/react-components';
import Chat from './components/chat/Chat';
import Header from './components/header/Header';
import { Loading, Login } from './components/views';
import { AuthHelper } from './libs/auth/AuthHelper';
import { useAppSelector } from './redux/app/hooks';
import { RootState } from './redux/app/store';
import { FeatureKeys } from './redux/features/app/AppState';
import { semanticKernelDarkTheme, semanticKernelLightTheme } from './styles';
import { useAppLoader } from './libs/hooks/useAppLoader';

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

/**
 * The root component of the application.
 *
 * Note: All of the internal loading logic is handled by the useAppLoader hook.
 *
 * @returns {JSX.Element}
 */
const App = (): JSX.Element => {
    const classes = useClasses();
    const [appState, setAppState] = useAppLoader();
    const { features } = useAppSelector((state: RootState) => state.app);

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

export default App;
