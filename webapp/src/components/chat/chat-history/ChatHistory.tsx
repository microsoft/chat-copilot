// Copyright (c) Microsoft. All rights reserved.

import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import React from 'react';
import { GetResponseOptions } from '../../../libs/hooks/useChat';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import { ChatHistoryItem } from './ChatHistoryItem';

const useClasses = makeStyles({
    root: {
        ...shorthands.gap(tokens.spacingVerticalM),
        display: 'flex',
        flexDirection: 'column',
        maxWidth: '900px',
        width: '100%',
        justifySelf: 'center',
    },
    item: {
        display: 'flex',
        flexDirection: 'column',
    },
});

interface ChatHistoryProps {
    messages: IChatMessage[];
    onGetResponse: (options: GetResponseOptions) => Promise<void>;
}

export const ChatHistory: React.FC<ChatHistoryProps> = ({ messages, onGetResponse }) => {
    const classes = useClasses();

    return (
        <div className={classes.root}>
            {messages.map((message, index) => (
                <ChatHistoryItem
                    key={message.timestamp}
                    message={message}
                    getResponse={onGetResponse}
                    messageIndex={index}
                />
            ))}
        </div>
    );
};
