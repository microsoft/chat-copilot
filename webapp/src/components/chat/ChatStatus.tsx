// Copyright (c) Microsoft. All rights reserved.

import { makeStyles } from '@fluentui/react-components';
import { Animation } from '@fluentui/react-northstar';
import React from 'react';
import { IChatUser } from '../../libs/models/ChatUser';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { TypingIndicator } from './typing-indicator/TypingIndicator';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'row',
    },
});

export const ChatStatus: React.FC = () => {
    const classes = useClasses();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { users } = conversations[selectedId];
    const { activeUserInfo } = useAppSelector((state: RootState) => state.app);
    const [typingUserList, setTypingUserList] = React.useState<IChatUser[]>([]);

    React.useEffect(() => {
        const checkAreTyping = () => {
            const updatedTypingUsers: IChatUser[] = users.filter(
                (chatUser: IChatUser) => chatUser.id !== activeUserInfo?.id && chatUser.isTyping,
            );

            setTypingUserList(updatedTypingUsers);
        };
        checkAreTyping();
    }, [activeUserInfo, users]);

    let message = conversations[selectedId].botResponseStatus;
    const numberOfUsersTyping = typingUserList.length;
    if (numberOfUsersTyping === 1) {
        message = message ? `${message} and a user is typing` : 'A user is typing';
    } else if (numberOfUsersTyping > 1) {
        message = message
            ? `${message} and ${numberOfUsersTyping} users are typing`
            : `${numberOfUsersTyping} users are typing`;
    }

    if (!message) {
        return null;
    }

    return (
        <Animation name="slideInCubic" keyframeParams={{ distance: '2.4rem' }}>
            <div className={classes.root}>
                <label>{message}</label>
                <TypingIndicator />
            </div>
        </Animation>
    );
};
