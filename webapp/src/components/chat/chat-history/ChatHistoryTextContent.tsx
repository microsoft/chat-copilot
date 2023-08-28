// Copyright (c) Microsoft. All rights reserved.

import { makeStyles } from '@fluentui/react-components';
import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import * as utils from './../../utils/TextUtils';

const useClasses = makeStyles({
    content: {
        wordBreak: 'break-word',
    },
});

interface ChatHistoryTextContentProps {
    message: IChatMessage;
}

export const ChatHistoryTextContent: React.FC<ChatHistoryTextContentProps> = ({ message }) => {
    const classes = useClasses();

    let content = message.content.trim().replace(/[\u00A0-\u9999<>&]/g, function (i: string) {
        return `&#${i.charCodeAt(0)};`;
    });
    content = utils.formatChatTextContent(content);
    return (
        <div className={classes.content}>
            <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
        </div>
    );
};
