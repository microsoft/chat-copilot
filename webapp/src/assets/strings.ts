export const COPY = {
    STEPWISE_RESULT_NOT_FOUND_REGEX: /(Result not found, review _stepsTaken to see what happened\.)\s+(\[{.*}])/g,
    CHAT_DELETED_MESSAGE: (chatName?: string) =>
        `Chat ${
            chatName ? `{${chatName}} ` : ''
        }has been removed by another user. You can still access the latest chat history for now. All chat content will be cleared once you refresh or exit the application.`,
    REFRESH_APP_ADVISORY: 'Please refresh the page to ensure you have the latest data.',
};
