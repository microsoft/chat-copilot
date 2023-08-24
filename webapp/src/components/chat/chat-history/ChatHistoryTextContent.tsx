// Copyright (c) Microsoft. All rights reserved.

import { makeStyles } from '@fluentui/react-components';
import React from 'react';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import * as utils from './../../utils/TextUtils';
import { convertToAnchorTags } from './../../utils/TextUtils';

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
    content = content.replace(/\n/g, '<br />').replace(/ {2}/g, '&nbsp;&nbsp;');

    if (message.citations && message.citations.length > 0) {
        content += '<br /><br />';
        message.citations.forEach((citation, index) => {
            content += `<span class='citation'>Source ${index + 1}: <a href='${citation.link}'>${
                citation.sourceName
            }</a></span><br />`;
        });
    }

    return <div className={classes.content} dangerouslySetInnerHTML={{ __html: convertToAnchorTags(content) }} />;
};
