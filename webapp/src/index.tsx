import { AccountInfo, PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { FluentProvider } from '@fluentui/react-components';
import ReactDOM from 'react-dom/client';
import { Provider as ReduxProvider } from 'react-redux';
import App from './App';
import { Constants } from './Constants';
import './index.css';
import { AuthHelper } from './libs/auth/AuthHelper';
import { store } from './redux/app/store';

import React from 'react';
import { ConfigService } from './libs/services/ConfigService';
import { setAuthConfig } from './redux/features/app/appSlice';
import { semanticKernelLightTheme } from './styles';

if (!localStorage.getItem('debug')) {
    localStorage.setItem('debug', `${Constants.debug.root}:*`);
}

let container: HTMLElement | null = null;
let msalInstance: PublicClientApplication | undefined;

document.addEventListener('DOMContentLoaded', () => {
    if (!container) {
        container = document.getElementById('root');
        if (!container) {
            throw new Error('Could not find root element');
        }
        const root = ReactDOM.createRoot(container);

        const configService = new ConfigService();
        const storedAuthConfig = store.getState().app.authConfig;

        // only fetch the auth config if we don't have it already
        const promise = storedAuthConfig ? Promise.resolve(storedAuthConfig) : configService.getAuthConfig();
        promise
            .then((authConfig) => {
                if (!storedAuthConfig) {
                    // if the auth config was fetched, set it in the store
                    store.dispatch(setAuthConfig(authConfig));
                }

                if (AuthHelper.isAuthAAD()) {
                    if (!msalInstance) {
                        msalInstance = new PublicClientApplication(AuthHelper.getMsalConfig(authConfig));
                        void msalInstance.handleRedirectPromise().then((response) => {
                            msalInstance?.setActiveAccount(response?.account as AccountInfo | null);
                        });
                    }

                    // render with the MsalProvider if AAD is enabled
                    root.render(
                        <React.StrictMode>
                            <ReduxProvider store={store}>
                                {/* eslint-disable-next-line @typescript-eslint/no-non-null-assertion */}
                                <MsalProvider instance={msalInstance}>
                                    <AppWithTheme />
                                </MsalProvider>
                            </ReduxProvider>
                        </React.StrictMode>,
                    );
                }
            })
            .catch(() => {
                store.dispatch(setAuthConfig(undefined));
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
