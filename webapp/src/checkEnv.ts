import { AuthType } from './libs/auth/AuthHelper';

/**
 * Checks if all required environment variables are defined
 * @returns {string[]} An array of missing environment variables
 */
export const getMissingEnvVariables = () => {
    // Should be aligned with variables defined in .env.example
    const envVariables = ['REACT_APP_BACKEND_URI'];
    const missingVariables = [];

    for (const variable of envVariables) {
        if (!process.env[variable]) {
            missingVariables.push(variable);
        }
    }

    if (process.env.REACT_APP_AUTH_TYPE === AuthType.AAD) {
        const aadVariables = ['REACT_APP_AAD_AUTHORITY', 'REACT_APP_AAD_CLIENT_ID'];
        for (const variable of aadVariables) {
            if (!process.env[variable]) {
                missingVariables.push(variable);
            }
        }
    }

    return missingVariables;
};
