// Copyright (c) Microsoft. All rights reserved.

import {
    Caption1,
    Card,
    CardHeader,
    ProgressBar,
    Text,
    makeStyles,
    mergeClasses,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import React from 'react';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import { SharedStyles } from '../../../styles';
import { getFileIconByFileExtension } from '../tabs/DocumentsTab';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.margin(tokens.spacingVerticalM, 0),
    },
    card: {
        height: 'fit-content',
        width: '275px',
        backgroundColor: tokens.colorNeutralBackground3,
        ...shorthands.gap(0),
        ...shorthands.margin(tokens.spacingVerticalXXS, 0),
        ...shorthands.padding(tokens.spacingVerticalXXS, 0),
    },
    cardCaption: {
        color: tokens.colorNeutralForeground2,
    },
    cardHeader: {
        ...shorthands.margin(0, tokens.spacingHorizontalS),
    },
    cardHeaderText: {
        fontSize: 'small',
        fontWeight: '500',
    },
    cardHeaderTextContainer: {
        ...SharedStyles.overflowEllipsis,
    },
    footer: {
        float: 'right',
        fontSize: 'small',
        fontWeight: '500',
        color: tokens.colorNeutralForegroundDisabled,
    },
    icon: {
        height: '32px',
        width: '32px',
    },
    floatLeft: {
        float: 'left',
    },
});

interface ChatHistoryDocumentContentProps {
    isMe: boolean;
    message: IChatMessage;
}

interface DocumentData {
    name: string;
    size: string;
    isUploaded: boolean;
}

interface DocumentMessageContent {
    documents: DocumentData[];
}

export const ChatHistoryDocumentContent: React.FC<ChatHistoryDocumentContentProps> = ({ isMe, message }) => {
    const classes = useClasses();

    let documents: DocumentData[] = [];
    try {
        ({ documents } = JSON.parse(message.content) as DocumentMessageContent);
    } catch (e) {
        console.error('Error parsing chat history file item: ' + message.content);
    }

    return (
        <>
            {documents.map(({ name, size, isUploaded }, index) => (
                <div className={classes.root} key={`${message.id ?? 'unknown-message-id'}-document-${index}`}>
                    <Card appearance="filled-alternative" className={classes.card}>
                        <CardHeader
                            className={classes.cardHeader}
                            image={getFileIconByFileExtension(name, { className: classes.icon })}
                            header={{
                                className: classes.cardHeaderTextContainer,
                                children: (
                                    <Text className={classes.cardHeaderText} title={name}>
                                        {name}
                                    </Text>
                                ),
                            }}
                            description={
                                <Caption1 block className={classes.cardCaption}>
                                    {size}
                                </Caption1>
                            }
                        />
                        <ProgressBar thickness="large" color={isUploaded ? 'success' : 'error'} value={1} />
                    </Card>
                    <span className={isMe ? classes.footer : mergeClasses(classes.footer, classes.floatLeft)}>
                        {isUploaded ? 'Success: memory established' : 'Failed: memory not established'}
                    </span>
                </div>
            ))}
        </>
    );
};
