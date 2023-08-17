export const COPY = {
    STEPWISE_RESULT_NOT_FOUND_REGEX: /(Result not found, review _stepsTaken to see what happened\.)\s+(\[{.*}])/g,
    CHAT_DELETED_MESSAGE: (chatName?: string) =>
        `Chat ${
            chatName ? `{${chatName}} ` : ''
        }has been deleted by another user. Please save any resources you need and refresh the page.`,
    REFRESH_APP_ADVISORY: 'Please refresh the page to ensure you have the latest data.',
};
