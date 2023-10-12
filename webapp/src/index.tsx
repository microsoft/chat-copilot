import { PublicClientApplication } from '@azure/msal-browser';
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
let root: ReactDOM.Root | undefined = undefined;
let msalInstance: PublicClientApplication | undefined;

document.addEventListener('DOMContentLoaded', () => {
    if (!container) {
        container = document.getElementById('root');
        if (!container) {
            throw new Error('Could not find root element');
        }
        root = ReactDOM.createRoot(container);

        renderApp();
    }
});

export function renderApp() {
    fetch(new URL('authConfig', BackendServiceUrl))
        .then((response) => (response.ok ? (response.json() as Promise<AuthConfig>) : Promise.reject()))
        .then((authConfig) => {
            store.dispatch(setAuthConfig(authConfig));

            if (AuthHelper.isAuthAAD()) {
                if (!msalInstance) {
                    msalInstance = new PublicClientApplication(AuthHelper.getMsalConfig(authConfig));
                    void msalInstance.handleRedirectPromise().then((response) => {
                        if (response) {
                            msalInstance?.setActiveAccount(response.account);
                        }
                    });
                }

                // render with the MsalProvider if AAD is enabled
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                root!.render(
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
        .catch(() => {
            store.dispatch(setAuthConfig(undefined));
        });

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    root!.render(
        <React.StrictMode>
            <ReduxProvider store={store}>
                <App />
            </ReduxProvider>
        </React.StrictMode>,
    );
}
