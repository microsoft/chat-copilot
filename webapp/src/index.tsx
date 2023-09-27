import { PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { FluentProvider } from '@fluentui/react-components';
import ReactDOM from 'react-dom/client';
import { Provider as ReduxProvider } from 'react-redux';
import App from './App';
import { Constants } from './Constants';
import './index.css';
import { AuthConfig, AuthHelper, AuthType } from './libs/auth/AuthHelper';
import { store } from './redux/app/store';

import React from 'react';
import { ConfigService } from './libs/services/ConfigService';
import { setAuthConfig } from './redux/features/app/appSlice';
import { semanticKernelLightTheme } from './styles';

if (!localStorage.getItem('debug')) {
    localStorage.setItem('debug', `${Constants.debug.root}:*`);
}

let container: HTMLElement | null = null;

document.addEventListener('DOMContentLoaded', () => {
    if (!container) {
        container = document.getElementById('root');
        if (!container) {
            throw new Error('Could not find root element');
        }
        const root = ReactDOM.createRoot(container);
        const configService = new ConfigService();

        configService
            .getAuthConfig()
            .then((authConfig) => {
                store.dispatch(setAuthConfig(authConfig));

                let msalInstance: PublicClientApplication | undefined;
                const isAuthAAD = authConfig.authType === AuthType.AAD;
                if (isAuthAAD) {
                    msalInstance = new PublicClientApplication(AuthHelper.msalConfig);
                    void msalInstance.handleRedirectPromise().then((response) => {
                        if (response) {
                            msalInstance?.setActiveAccount(response.account);
                        }
                    });

                    root.render(
                        <React.StrictMode>
                            <ReduxProvider store={store}>
                                <MsalProvider instance={msalInstance}>
                                    <AppWithTheme />
                                </MsalProvider>
                            </ReduxProvider>
                        </React.StrictMode>,
                    );
                }
            })
            .catch(() => {
                store.dispatch(setAuthConfig({} as AuthConfig));
            });

        root.render(
            <React.StrictMode>
                <ReduxProvider store={store}>
                    <AppWithTheme />
                </ReduxProvider>
            </React.StrictMode>,
        );
    }
});

const AppWithTheme = () => {
    return (
        <FluentProvider className="app-container" theme={semanticKernelLightTheme}>
            <App />
        </FluentProvider>
    );
};
