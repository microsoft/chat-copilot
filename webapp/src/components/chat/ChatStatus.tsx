// Copyright (c) Microsoft. All rights reserved.

import { makeStyles } from '@fluentui/react-components';
import React, { useMemo } from 'react';
import { IChatUser } from '../../libs/models/ChatUser';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { ChatState } from '../../redux/features/conversations/ChatState';
import { TypingIndicator } from './typing-indicator/TypingIndicator';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'row',
    },
});

interface ChatStatusProps {
    chatState: ChatState;
}
/**
 * Component to display the chat status, including the number of users typing and the bot response status.
 *
 * @returns {*} The chat status component
 */
export const ChatStatus: React.FC<ChatStatusProps> = (props: ChatStatusProps) => {
    const classes = useClasses();
    const { activeUserInfo } = useAppSelector((state: RootState) => state.app);

    // The last message either from the user or the bot
    const lastMessage = props.chatState.messages[props.chatState.messages.length - 1];

    // Get the bot response status if the last message is not from the bot
    const botResponseStatus = () => {
        if (props.chatState.messages.length < 1) {
            return undefined;
        }
        return (lastMessage.userId === 'Bot' || lastMessage.userName === 'Bot') && Boolean(lastMessage.content.length)
            ? undefined
            : props.chatState.botResponseStatus;
    };

    // The number of users typing in the chat
    const numUsersTyping = useMemo(() => {
        return props.chatState.users.filter(
            (chatUser: IChatUser) => chatUser.id !== activeUserInfo?.id && chatUser.isTyping,
        ).length;
    }, [activeUserInfo?.id, props.chatState.users]);

    /**
     * Get the status message to display.
     *
     * @param {number} numUsersTyping
     * @param {string} [botStatus] - The bot response status
     * @returns {string | undefined} Status message
     */
    const getStatus = (numUsersTyping: number, botStatus?: string) => {
        if (numUsersTyping === 1 && botStatus) {
            return `${botStatus} and a user is typing`;
        }
        if (numUsersTyping === 1) {
            return 'A user is typing';
        }
        if (numUsersTyping > 1 && botStatus) {
            return `${botStatus} and ${numUsersTyping} users are typing`;
        }
        if (numUsersTyping > 1) {
            return `${numUsersTyping} users are typing`;
        }
        return botStatus;
    };

    if (!getStatus(numUsersTyping, botResponseStatus())) {
        return null;
    }

    return (
        <div className={classes.root}>
            <label style={{ marginRight: '4px' }}>{getStatus(numUsersTyping, botResponseStatus())}</label>
            <TypingIndicator />
        </div>
    );
};
