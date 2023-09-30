import { AccountInfo, PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import ReactDOM from 'react-dom/client';
import { Provider as ReduxProvider } from 'react-redux';
import App from './App';
import { Constants } from './Constants';
import './index.css';
import { AuthConfig, AuthHelper } from './libs/auth/AuthHelper';
import { store } from './redux/app/store';

import React from 'react';
import { BackendServiceUrl } from './libs/services/BaseService';
import { setAuthConfig } from './redux/features/app/appSlice';

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

        // only fetch the auth config if we don't have it already
        const storedAuthConfig = AuthHelper.getAuthConfig();
        const promise =
            storedAuthConfig && Object.keys(storedAuthConfig).length
                ? Promise.resolve(storedAuthConfig)
                : fetch(new URL('authConfig', BackendServiceUrl)).then((response) =>
                      response.ok ? (response.json() as Promise<AuthConfig>) : Promise.reject(),
                  );

        promise
            .then((authConfig) => {
                store.dispatch(setAuthConfig(authConfig));

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
                                <MsalProvider instance={msalInstance}>
                                    <App />
                                </MsalProvider>
                            </ReduxProvider>
                        </React.StrictMode>,
                    );
                }
            })
            .catch((e: unknown) => {
                if (e instanceof TypeError) {
                    // fetch() will reject with a TypeError when a network error is encountered.
                    store.dispatch(setAuthConfig(null));
                } else {
                    store.dispatch(setAuthConfig(undefined));
                }
            });

        root.render(
            <React.StrictMode>
                <ReduxProvider store={store}>
                    <App />
                </ReduxProvider>
            </React.StrictMode>,
        );
    }
});
