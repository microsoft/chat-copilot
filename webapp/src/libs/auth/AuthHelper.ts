// Copyright (c) Microsoft. All rights reserved.

import {
    Configuration,
    EndSessionRequest,
    IPublicClientApplication,
    InteractionStatus,
    LogLevel,
} from '@azure/msal-browser';
import debug from 'debug';
import { Constants } from '../../Constants';
import { store } from '../../redux/app/store';
import { TokenHelper } from './TokenHelper';

const log = debug(Constants.debug.root).extend('authHelper');

export const enum AuthType {
    None = 'None',
    AAD = 'AzureAd',
}

export interface AuthConfig {
    authType: AuthType;
    aadAuthority: string;
    aadClientId: string;
    aadApiScope: string;
}

const getMsalConfig = (authConfig: AuthConfig): Configuration => ({
    auth: {
        clientId: authConfig.aadClientId,
        authority: authConfig.aadAuthority,
        redirectUri: window.origin,
    },
    cache: Constants.msal.cache,
    system: {
        loggerOptions: {
            loggerCallback: (level: LogLevel, message: string, containsPii: boolean) => {
                if (containsPii) {
                    return;
                }
                switch (level) {
                    case LogLevel.Error:
                        log('error:', message);
                        return;
                    case LogLevel.Info:
                        log('info:', message);
                        return;
                    case LogLevel.Verbose:
                        log('verbose:', message);
                        return;
                    case LogLevel.Warning:
                        log('warn:', message);
                        return;
                    default:
                        log(message);
                }
            },
        },
        windowHashTimeout: 9000, // Applies just to popup calls - In milliseconds
        iframeHashTimeout: 9000, // Applies just to silent calls - In milliseconds
        loadFrameTimeout: 9000, // Applies to both silent and popup calls - In milliseconds
    },
});

const getMsalScopes = () => {
    const aadApiScope = getAuthConfig()?.aadApiScope;
    return Constants.msal.semanticKernelScopes.concat(aadApiScope ?? []);
};

const logoutRequest: EndSessionRequest = {
    postLogoutRedirectUri: window.origin,
};

const ssoSilentRequest = async (msalInstance: IPublicClientApplication) => {
    await msalInstance.ssoSilent({
        account: msalInstance.getActiveAccount() ?? undefined,
        scopes: getMsalScopes(),
    });
};

const loginAsync = async (instance: IPublicClientApplication) => {
    if (Constants.msal.method === 'redirect') {
        await instance.loginRedirect({
            scopes: getMsalScopes(),
            extraScopesToConsent: Constants.msal.msGraphAppScopes,
        });
    } else {
        await instance.loginPopup({
            scopes: getMsalScopes(),
            extraScopesToConsent: Constants.msal.msGraphAppScopes,
        });
    }
};

const logoutAsync = (instance: IPublicClientApplication) => {
    if (Constants.msal.method === 'popup') {
        void instance.logoutPopup(logoutRequest);
    }
    if (Constants.msal.method === 'redirect') {
        void instance.logoutRedirect(logoutRequest);
    }
};

const getAuthConfig = () => store.getState().app.authConfig;
const isAuthAAD = () => getAuthConfig()?.authType === AuthType.AAD;

// SKaaS = Semantic Kernel as a Service
// Gets token with scopes to authorize SKaaS specifically
const getSKaaSAccessToken = async (instance: IPublicClientApplication, inProgress: InteractionStatus) => {
    return isAuthAAD() ? await TokenHelper.getAccessTokenUsingMsal(inProgress, instance, getMsalScopes()) : '';
};

export const AuthHelper = {
    getSKaaSAccessToken,
    getMsalConfig,
    getMsalScopes,
    logoutRequest,
    ssoSilentRequest,
    loginAsync,
    logoutAsync,
    isAuthAAD,
    getAuthConfig,
};
