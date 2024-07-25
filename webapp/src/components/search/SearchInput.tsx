// Copyright (c) Microsoft. All rights reserved.
import { Button, Textarea, makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { SendRegular } from '@fluentui/react-icons';
import debug from 'debug';
import React, { useState } from 'react';
import { Constants } from '../../Constants';
import { AlertType } from '../../libs/models/AlertType';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { addAlert } from '../../redux/features/app/appSlice';
import { Alerts } from '../shared/Alerts';
import { updateUserIsTyping } from '../../redux/features/conversations/conversationsSlice';

const log = debug(Constants.debug.root).extend('chat-input');

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        width: '95%',
        maxWidth: '105em',
        justifyContent: 'center',
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

interface SearchInputProps {
    onSubmit: (value: string) => Promise<void>;
}

export const SearchInput: React.FC<SearchInputProps> = ({ onSubmit }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { activeUserInfo } = useAppSelector((state: RootState) => state.app);
    const [value, setValue] = useState('');

    const textAreaRef = React.useRef<HTMLTextAreaElement>(null);

    const handleSubmit = (value: string) => {
        console.log(value)
        if (value.trim() === '') {
            return; // only submit if value is not empty
        }
        onSubmit(value).catch((error) => {
            const message = `Error submitting search input: ${(error as Error).message}`;
            log(message);
            dispatch(
                addAlert({
                    type: AlertType.Error,
                    message,
                }),
            );
        });
        setValue('');
    };

    return (
        <div className={classes.root}>
            <Alerts />
            <div className={classes.content}>
            <Textarea
                    title="Search input"
                    aria-label="Search input field. Click enter to submit input."
                    ref={textAreaRef}
                    id="search-input"
                    resize="vertical"
                    textarea={{
                        className: classes.textarea,
                    }}
                    className={classes.input}
                    value={value}
                    onFocus={() => {
                        // update the locally stored value to the current value
                        const searchInput = document.getElementById('search-input');
                        if (searchInput) {
                            setValue((searchInput as HTMLTextAreaElement).value);
                        }
                        // User is considered typing if the input is in focus
                        if (activeUserInfo) {
                            dispatch(
                                updateUserIsTyping({ userId: activeUserInfo.id, chatId: selectedId, isTyping: true }),
                            );
                        }
                    }}
                    onChange={(_event, data) => {
                        setValue(data.value);
                    }}
                    onKeyDown={(event) => {
                        if (event.key === 'Enter' && !event.shiftKey) {
                            event.preventDefault();
                            handleSubmit(value);
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
                <div className={classes.essentials}>
                    <Button
                        title="Submit"
                        aria-label="Submit message"
                        appearance="transparent"
                        icon={<SendRegular />}
                        onClick={() => {
                            handleSubmit(value);
                        }}
                        disabled={conversations[selectedId].disabled}
                    />
                </div>
            </div>
        </div>
    );
};
