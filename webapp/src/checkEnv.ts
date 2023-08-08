// Checks if all required environment variables are defined
// Returns an array of missing variables
export const getMissingEnvVariables = () => {
    // Should be aligned with variables defined in .env.example
    const envVariables = ['REACT_APP_BACKEND_URI', 'REACT_APP_AAD_AUTHORITY', 'REACT_APP_AAD_CLIENT_ID'];
    const missingVariables = [];

    for (const variable of envVariables) {
        if (!process.env[variable]) {
            missingVariables.push(variable);
        }
    }

    return missingVariables;
};
