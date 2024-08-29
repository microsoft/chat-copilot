// Copyright (c) Microsoft. All rights reserved.

import {
    AvatarProps,
    Button,
    Persona,
    Text,
    ToggleButton,
    Tooltip,
    makeStyles,
    mergeClasses,
    shorthands,
} from '@fluentui/react-components';
import {
    ChevronDown20Regular,
    ChevronUp20Regular,
    Clipboard20Regular,
    ClipboardTask20Regular,
} from '@fluentui/react-icons';
import React, { useState } from 'react';
import { useChat } from '../../../libs/hooks/useChat';
import { AuthorRoles, ChatMessageType, IChatMessage } from '../../../libs/models/ChatMessage';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { FeatureKeys } from '../../../redux/features/app/AppState';
import { Breakpoints, customTokens } from '../../../styles';
import { timestampToDateString } from '../../utils/TextUtils';
import { PlanViewer } from '../plan-viewer/PlanViewer';
import { PromptDialog } from '../prompt-dialog/PromptDialog';
import { TypingIndicator } from '../typing-indicator/TypingIndicator';
import * as utils from './../../utils/TextUtils';
import { ChatHistoryDocumentContent } from './ChatHistoryDocumentContent';
import { ChatHistoryTextContent } from './ChatHistoryTextContent';
import { CitationCards } from './CitationCards';
import { UserFeedbackActions } from './UserFeedbackActions';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'row',
        maxWidth: '75%',
        minWidth: '24em',
        ...shorthands.borderRadius(customTokens.borderRadiusMedium),
        ...Breakpoints.small({
            maxWidth: '100%',
        }),
        ...shorthands.gap(customTokens.spacingHorizontalXS),
    },
    debug: {
        position: 'absolute',
        top: '-4px',
        right: '-4px',
    },
    alignEnd: {
        alignSelf: 'flex-end',
    },
    persona: {
        paddingTop: customTokens.spacingVerticalS,
    },
    item: {
        backgroundColor: customTokens.colorNeutralBackground1,
        ...shorthands.borderRadius(customTokens.borderRadiusMedium),
        ...shorthands.padding(customTokens.spacingVerticalXS, customTokens.spacingHorizontalS),
    },
    me: {
        backgroundColor: customTokens.colorMeBackground,
        width: '100%',
    },
    time: {
        color: customTokens.colorNeutralForeground3,
        fontSize: customTokens.fontSizeBase200,
        fontWeight: 400,
    },
    header: {
        position: 'relative',
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        ...shorthands.gap(customTokens.spacingHorizontalL),
    },
    headerMenu: {
        display: 'flex',
        flexDirection: 'row',
        marginLeft: 'auto',
    },
    canvas: {
        width: '100%',
        textAlign: 'center',
    },
    image: {
        maxWidth: '250px',
    },
    blur: {
        filter: 'blur(5px)',
    },
    controls: {
        marginTop: customTokens.spacingVerticalS,
        marginBottom: customTokens.spacingVerticalS,
        ...shorthands.gap(customTokens.spacingHorizontalL),
    },
    citationButton: {
        marginRight: 'auto',
    },
    rlhf: {
        marginLeft: 'auto',
    },
});

interface ChatHistoryItemProps {
    message: IChatMessage;
    messageIndex: number;
}

export const ChatHistoryItem: React.FC<ChatHistoryItemProps> = ({ message, messageIndex }) => {
    const classes = useClasses();
    const chat = useChat();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { activeUserInfo, features } = useAppSelector((state: RootState) => state.app);
    const { chatSpecialization } = useAppSelector((state: RootState) => state.admin);

    const [showCitationCards, setShowCitationCards] = useState(false);

    const isMe = message.authorRole === AuthorRoles.User && message.userId === activeUserInfo?.id;
    const isBot = message.authorRole === AuthorRoles.Bot;
    const user = chat.getChatUserById(message.userName, selectedId, conversations[selectedId].users);
    const fullName = user?.fullName ?? message.userName;

    const [messagedCopied, setMessageCopied] = useState(false);

    const copyOnClick = async () => {
        await navigator.clipboard.writeText(message.content).then(() => {
            setMessageCopied(true);
            setTimeout(() => {
                setMessageCopied(false);
            }, 2000);
        });
    };

    const avatarImage = activeUserInfo?.image
        ? {
              src: activeUserInfo.image,
          }
        : undefined;

    const avatar: AvatarProps = isBot
        ? {
              image: {
                  src: chatSpecialization?.iconFilePath
                      ? chatSpecialization.iconFilePath
                      : conversations[selectedId].botProfilePicture,
              },
          }
        : { name: fullName, color: 'colorful', image: avatarImage };

    let content: JSX.Element;
    if (isBot && message.type === ChatMessageType.Plan) {
        content = <PlanViewer message={message} messageIndex={messageIndex} />;
    } else if (message.type === ChatMessageType.Document) {
        content = <ChatHistoryDocumentContent isMe={isMe} message={message} />;
    } else {
        content =
            isBot && message.content.length === 0 ? <TypingIndicator /> : <ChatHistoryTextContent message={message} />;
    }

    const showFeedback = features[FeatureKeys.RLHF].enabled && message.userId === 'Bot' && message.content.length > 0;

    const messageCitations = message.citations ?? [];
    const showMessageCitation = messageCitations.length > 0;

    return (
        <div
            className={isMe ? mergeClasses(classes.root, classes.alignEnd) : classes.root}
            // The following data attributes are needed for CI and testing
            data-testid={`chat-history-item-${messageIndex}`}
            data-username={fullName}
            data-content={utils.formatChatTextContent(message.content)}
        >
            {
                <Persona
                    className={classes.persona}
                    avatar={avatar}
                    presence={
                        !features[FeatureKeys.SimplifiedExperience].enabled && !isMe
                            ? { status: 'available' }
                            : undefined
                    }
                />
            }
            <div className={isMe ? mergeClasses(classes.item, classes.me) : classes.item}>
                <div className={classes.header}>
                    {!isMe && <Text weight="semibold">{fullName}</Text>}
                    <Text className={classes.time}>{timestampToDateString(message.timestamp, true)}</Text>
                    <div className={classes.headerMenu}>
                        {showFeedback && message.id && (
                            <div className={classes.rlhf}>
                                {<UserFeedbackActions messageId={message.id} wasHelpful={message.userFeedback} />}
                            </div>
                        )}
                        {isBot && <PromptDialog message={message} />}
                        <Tooltip content={messagedCopied ? 'Copied' : 'Copy text'} relationship="label">
                            <Button
                                icon={messagedCopied ? <ClipboardTask20Regular /> : <Clipboard20Regular />}
                                appearance="transparent"
                                onClick={() => {
                                    void copyOnClick();
                                }}
                            />
                        </Tooltip>
                    </div>
                </div>
                <div className="message-content">{content}</div>

                <div className={classes.controls}>
                    {showMessageCitation && (
                        <ToggleButton
                            appearance="subtle"
                            checked={showCitationCards}
                            className={classes.citationButton}
                            icon={showCitationCards ? <ChevronUp20Regular /> : <ChevronDown20Regular />}
                            iconPosition="after"
                            onClick={() => {
                                setShowCitationCards(!showCitationCards);
                            }}
                            size="small"
                        >
                            {`${messageCitations.length} ${messageCitations.length === 1 ? 'citation' : 'citations'}`}
                        </ToggleButton>
                    )}
                    {showCitationCards && <CitationCards message={message} />}
                </div>
            </div>
        </div>
    );
};
