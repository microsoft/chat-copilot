// Copyright (c) Microsoft. All rights reserved.

import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import debug from 'debug';
import React from 'react';
import { Constants } from '../../Constants';
import { GetResponseOptions, useChat } from '../../libs/hooks/useChat';
import { AuthorRoles, IChatMessage } from '../../libs/models/ChatMessage';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { FeatureKeys, Features } from '../../redux/features/app/AppState';
import { addMessageToConversationFromUser } from '../../redux/features/conversations/conversationsSlice';
import { SharedStyles } from '../../styles';
import { ChatInput } from './ChatInput';
import { ChatHistory } from './chat-history/ChatHistory';

const log = debug(Constants.debug.root).extend('chat-room');

const useClasses = makeStyles({
    root: {
        ...shorthands.overflow('hidden'),
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        height: '100%',
    },
    scroll: {
        ...shorthands.margin(tokens.spacingVerticalXS),
        ...SharedStyles.scroll,
    },
    history: {
        ...shorthands.padding(tokens.spacingVerticalM),
        marginLeft: '40px',
        paddingRight: '40px',
        display: 'flex',
        justifyContent: 'center',
    },
    input: {
        ...shorthands.padding(tokens.spacingVerticalM),
    },
});

export const ChatRoom: React.FC = () => {
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { activeUserInfo } = useAppSelector((state: RootState) => state.app);

    const messages = conversations[selectedId].messages;
    const classes = useClasses();

    const dispatch = useAppDispatch();
    const scrollViewTargetRef = React.useRef<HTMLDivElement>(null);
    const [shouldAutoScroll, setShouldAutoScroll] = React.useState(true);

    const [isDraggingOver, setIsDraggingOver] = React.useState(false);
    const onDragEnter = (e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        setIsDraggingOver(true);
    };
    const onDragLeave = (e: React.DragEvent<HTMLDivElement | HTMLTextAreaElement>) => {
        e.preventDefault();
        setIsDraggingOver(false);
    };

    const chat = useChat();

    React.useEffect(() => {
        if (!shouldAutoScroll) return;
        scrollViewTargetRef.current?.scrollTo(0, scrollViewTargetRef.current.scrollHeight);
    }, [messages, shouldAutoScroll]);

    React.useEffect(() => {
        const onScroll = () => {
            if (!scrollViewTargetRef.current) return;
            const { scrollTop, scrollHeight, clientHeight } = scrollViewTargetRef.current;
            const isAtBottom = scrollTop + clientHeight >= scrollHeight - 10;
            setShouldAutoScroll(isAtBottom);
        };

        if (!scrollViewTargetRef.current) return;

        const currentScrollViewTarget = scrollViewTargetRef.current;

        currentScrollViewTarget.addEventListener('scroll', onScroll);
        return () => {
            currentScrollViewTarget.removeEventListener('scroll', onScroll);
        };
    }, []);

    const handleSubmit = async (options: GetResponseOptions) => {
        log('submitting user chat message');

        const chatInput: IChatMessage = {
            chatId: selectedId,
            timestamp: new Date().getTime(),
            userId: activeUserInfo?.id as string,
            userName: activeUserInfo?.username as string,
            content: options.value,
            type: options.messageType,
            authorRole: AuthorRoles.User,
        };

        dispatch(addMessageToConversationFromUser({ message: chatInput, chatId: selectedId }));

        await chat.getResponse(options);

        setShouldAutoScroll(true);
    };

    if (conversations[selectedId].hidden) {
        return (
            <div className={classes.root}>
                <div className={classes.scroll}>
                    <div className={classes.history}>
                        <h3>
                            This conversation is not visible in the app because{' '}
                            {Features[FeatureKeys.MultiUserChat].label} is disabled. Please enable the feature in the
                            settings to view the conversation, select a different one, or create a new conversation.
                        </h3>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={classes.root} onDragEnter={onDragEnter} onDragOver={onDragEnter} onDragLeave={onDragLeave}>
            <div ref={scrollViewTargetRef} className={classes.scroll}>
                <div className={classes.history}>
                    <ChatHistory messages={messages} onGetResponse={handleSubmit} />
                </div>
            </div>
            <div className={classes.input}>
                <ChatInput isDraggingOver={isDraggingOver} onDragLeave={onDragLeave} onSubmit={handleSubmit} />
            </div>
        </div>
    );
};
