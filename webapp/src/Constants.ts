import botIcon1 from './assets/bot-icons/bot-icon-1.png';

export const Constants = {
    app: {
        name: 'Copilot',
        updateCheckIntervalSeconds: 60 * 5,
        CONNECTION_ALERT_ID: 'connection-alert',
        importTypes: '.txt,.pdf,.docx,.md,.jpg,.jpeg,.png,.tif,.tiff,.bmp,.gif',
    },
    msal: {
        method: 'redirect', // 'redirect' | 'popup'
        cache: {
            cacheLocation: 'localStorage',
            storeAuthStateInCookie: false,
        },
        semanticKernelScopes: ['openid', 'offline_access', 'profile'],
        // MS Graph scopes required for loading user information
        msGraphAppScopes: ['User.ReadBasic.All'],
    },
    bot: {
        profile: {
            id: 'bot',
            fullName: 'Copilot',
            emailAddress: '',
            photo: botIcon1,
        },
        fileExtension: 'skcb',
        typingIndicatorTimeoutMs: 5000,
    },
    debug: {
        root: 'sk-chatbot',
    },
    sk: {
        service: {
            defaultDefinition: 'int',
        },
        // Reserved context variable names
        reservedWords: ['server_url', 'server-url'],
        // Flag used to indicate that the variable is unknown in plan preview
        UNKNOWN_VARIABLE_FLAG: '$???',
    },
    adoScopes: ['vso.work'],
    BATCH_REQUEST_LIMIT: 20,
    plugins: {
        // For a list of Microsoft Graph permissions, see https://learn.microsoft.com/en-us/graph/permissions-reference.
        // Your application registration will need to be granted these permissions in Azure Active Directory.
        //msGraphScopes: ['Calendars.Read', 'Mail.Read', 'Mail.Send', 'Tasks.ReadWrite', 'User.Read'],
        msGraphScopes: ['Calendars.Read', 'Mail.Read', 'Tasks.ReadWrite', 'User.Read'],
    },
    KEYSTROKE_DEBOUNCE_TIME_MS: 250,
};
