// Copyright (c) Microsoft. All rights reserved.

import { useMsal } from '@azure/msal-react';
import { Button, Spinner, Textarea, makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { AttachRegular, MicRegular, SendRegular } from '@fluentui/react-icons';
import debug from 'debug';
import * as speechSdk from 'microsoft-cognitiveservices-speech-sdk';
import React, { useRef, useState } from 'react';
import { Constants } from '../../Constants';
import { COPY } from '../../assets/strings';
import { AuthHelper } from '../../libs/auth/AuthHelper';
import { useFile } from '../../libs/hooks';
import { GetResponseOptions } from '../../libs/hooks/useChat';
import { AlertType } from '../../libs/models/AlertType';
import { ChatMessageType } from '../../libs/models/ChatMessage';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { addAlert } from '../../redux/features/app/appSlice';
import { editConversationInput, updateBotResponseStatus } from '../../redux/features/conversations/conversationsSlice';
import { Alerts } from '../shared/Alerts';
import { getErrorDetails } from '../utils/TextUtils';
import { SpeechService } from './../../libs/services/SpeechService';
import { updateUserIsTyping } from './../../redux/features/conversations/conversationsSlice';
import { ChatStatus } from './ChatStatus';

const log = debug(Constants.debug.root).extend('chat-input');

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        width: '100%',
        maxWidth: '105em',
        ...shorthands.margin(tokens.spacingVerticalNone, tokens.spacingHorizontalM),
    },
    typingIndicator: {
        maxHeight: '28px',
    },
    content: {
        ...shorthands.gap(tokens.spacingHorizontalM),
        display: 'flex',
        flexDirection: 'row',
        width: '100%',
    },
    input: {
        width: '100%',
    },
    textarea: {
        maxHeight: '80px',
    },
    controls: {
        display: 'flex',
        flexDirection: 'row',
    },
    essentials: {
        display: 'flex',
        flexDirection: 'row',
        marginLeft: 'auto', // align to right
    },
    functional: {
        display: 'flex',
        flexDirection: 'row',
    },
    dragAndDrop: {
        ...shorthands.border(tokens.strokeWidthThick, ' solid', tokens.colorBrandStroke1),
        ...shorthands.padding('8px'),
        textAlign: 'center',
        backgroundColor: tokens.colorNeutralBackgroundInvertedDisabled,
        fontSize: tokens.fontSizeBase300,
        color: tokens.colorBrandForeground1,
        caretColor: 'transparent',
    },
});

interface ChatInputProps {
    isDraggingOver?: boolean;
    onDragLeave: React.DragEventHandler<HTMLDivElement | HTMLTextAreaElement>;
    onSubmit: (options: GetResponseOptions) => Promise<void>;
}

/**
 * Chat input component to allow users to type messages, attach files, and submit messages.
 *
 * @param {ChatInputProps} props
 * @returns {*} The chat input component
 */
export const ChatInput: React.FC<ChatInputProps> = ({ isDraggingOver, onDragLeave, onSubmit }) => {
    const classes = useClasses();
    const fileHandler = useFile();
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { specializations } = useAppSelector((state: RootState) => state.admin);
    const { activeUserInfo } = useAppSelector((state: RootState) => state.app);

    const [input, setInput] = useState('');
    const [recognizer, setRecognizer] = useState<speechSdk.SpeechRecognizer>();
    const [isListening, setIsListening] = useState(false);

    const documentFileRef = useRef<HTMLInputElement | null>(null);
    const textAreaRef = React.useRef<HTMLTextAreaElement>(null);

    // Current chat state
    const chatState = conversations[selectedId];

    React.useEffect(() => {
        // Focus on the text area when the selected conversation changes
        textAreaRef.current?.focus();
    }, [selectedId]);

    React.useEffect(() => {
        // Only initialize the speech recognizer once
        if (recognizer) {
            return;
        }

        async function initSpeechRecognizer() {
            const speechService = new SpeechService();
            const response = await speechService.getSpeechTokenAsync(
                await AuthHelper.getSKaaSAccessToken(instance, inProgress),
            );

            if (response.isSuccess) {
                const recognizer = speechService.getSpeechRecognizerAsyncWithValidKey(response);
                setRecognizer(recognizer);
            }
        }

        initSpeechRecognizer().catch((e) => {
            const errorDetails = getErrorDetails(e);
            const errorMessage = `Unable to initialize speech recognizer. Details: ${errorDetails}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        });
    }, [dispatch, instance, inProgress, recognizer]);

    React.useEffect(() => {
        // If the chat is disabled, set the input to the deleted message
        if (chatState.disabled) {
            setInput(COPY.CHAT_DELETED_MESSAGE());
            return;
        }

        if (chatState.input !== COPY.CHAT_DELETED_MESSAGE()) {
            return;
        }

        setInput('');
    }, [chatState.disabled, chatState.input]);

    const handleSpeech = () => {
        setIsListening(true);
        if (recognizer) {
            recognizer.recognizeOnceAsync((result) => {
                if (result.reason === speechSdk.ResultReason.RecognizedSpeech) {
                    if (result.text && result.text.length > 0) {
                        handleSubmit(result.text);
                    }
                }
                setIsListening(false);
            });
        }
    };

    const handleSubmit = (value: string, messageType: ChatMessageType = ChatMessageType.Message) => {
        if (value.trim() === '') {
            return; // only submit if value is not empty
        }

        // Update the conversation input on submit
        dispatch(editConversationInput({ id: selectedId, newInput: value }));

        // Reset the input field and conversation input
        setInput('');
        dispatch(editConversationInput({ id: selectedId, newInput: '' }));
        dispatch(updateBotResponseStatus({ chatId: selectedId, status: 'Calling the kernel' }));

        onSubmit({ value, messageType, chatId: selectedId }).catch((error) => {
            const message = `Error submitting chat input: ${(error as Error).message}`;
            log(message);
            dispatch(
                addAlert({
                    type: AlertType.Error,
                    message,
                }),
            );
        });
    };

    const handleDrop = (e: React.DragEvent<HTMLTextAreaElement>) => {
        onDragLeave(e);
        void fileHandler.handleImport(selectedId, documentFileRef, false, undefined, e.dataTransfer.files);
    };

    const isSpecializationDisabled = () => {
        if (conversations[selectedId].specializationId === '') {
            return true;
        } else {
            const specialization = specializations.find(
                (spec) => spec.id === conversations[selectedId].specializationId,
            );
            if (
                specialization &&
                specialization.id == conversations[selectedId].specializationId &&
                specialization.isActive
            ) {
                return false;
            }
        }
        return true;
    };

    return (
        <div className={classes.root}>
            <div className={classes.typingIndicator}>
                <ChatStatus chatState={chatState} />
            </div>
            <Alerts />
            <div className={classes.content}>
                <Textarea
                    title="Chat input"
                    aria-label="Chat input field. Click enter to submit input."
                    ref={textAreaRef}
                    id="chat-input"
                    resize="vertical"
                    disabled={chatState.disabled}
                    textarea={{
                        className: isDraggingOver
                            ? mergeClasses(classes.dragAndDrop, classes.textarea)
                            : classes.textarea,
                    }}
                    className={classes.input}
                    value={isDraggingOver ? 'Drop your files here' : input}
                    onDrop={handleDrop}
                    onFocus={() => {
                        // update the locally stored value to the current value
                        const chatInput = document.getElementById('chat-input');

                        if (chatInput) {
                            setInput((chatInput as HTMLTextAreaElement).value);
                        }
                        // User is considered typing if the input is in focus
                        if (activeUserInfo) {
                            dispatch(
                                updateUserIsTyping({ userId: activeUserInfo.id, chatId: selectedId, isTyping: true }),
                            );
                        }
                    }}
                    onChange={(_event, data) => {
                        if (isDraggingOver) {
                            return;
                        }

                        setInput(data.value);
                    }}
                    onKeyDown={(event) => {
                        if (event.key === 'Enter' && !event.shiftKey) {
                            event.preventDefault();
                            handleSubmit(input);
                        }
                    }}
                    onBlur={() => {
                        // User is considered not typing if the input is not  in focus
                        if (activeUserInfo) {
                            dispatch(
                                updateUserIsTyping({ userId: activeUserInfo.id, chatId: selectedId, isTyping: false }),
                            );
                        }
                    }}
                />
            </div>
            <div className={classes.controls}>
                <div className={classes.functional}>
                    {/* Hidden input for file upload. Only accept .txt and .pdf files for now. */}
                    <input
                        type="file"
                        ref={documentFileRef}
                        style={{ display: 'none' }}
                        accept={Constants.app.importTypes}
                        multiple={true}
                        onChange={() => {
                            void fileHandler.handleImport(selectedId, documentFileRef);
                        }}
                    />
                    <Button
                        disabled={
                            chatState.disabled ||
                            (chatState.importingDocuments && chatState.importingDocuments.length > 0)
                        }
                        appearance="transparent"
                        icon={<AttachRegular />}
                        onClick={() => documentFileRef.current?.click()}
                        title="Attach file"
                        aria-label="Attach file button"
                    />
                    {chatState.importingDocuments && chatState.importingDocuments.length > 0 && <Spinner size="tiny" />}
                </div>
                <div className={classes.essentials}>
                    {recognizer && (
                        <Button
                            appearance="transparent"
                            disabled={chatState.disabled || isListening}
                            icon={<MicRegular />}
                            onClick={handleSpeech}
                        />
                    )}
                    <Button
                        title="Submit"
                        aria-label="Submit message"
                        appearance="transparent"
                        icon={<SendRegular />}
                        onClick={() => {
                            handleSubmit(input);
                        }}
                        disabled={chatState.disabled || isSpecializationDisabled()}
                    />
                </div>
            </div>
        </div>
    );
};
