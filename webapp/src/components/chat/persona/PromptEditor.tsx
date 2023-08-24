// Copyright (c) Microsoft. All rights reserved.

import {
    Button,
    Popover,
    PopoverSurface,
    PopoverTrigger,
    Textarea,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import React from 'react';
import { AlertType } from '../../../libs/models/AlertType';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { addAlert } from '../../../redux/features/app/appSlice';
import { Info16 } from '../../shared/BundledIcons';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
    },
    horizontal: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
        alignItems: 'center',
    },
    controls: {
        display: 'flex',
        marginLeft: 'auto',
    },
    dialog: {
        maxWidth: '25%',
    },
});

// The number of rows in the textarea.
// Specifies the height in average character heights.
const Rows = 8;

interface PromptEditorProps {
    chatId: string;
    title: string;
    prompt: string;
    isEditable: boolean;
    info: string;
    modificationHandler?: (value: string) => Promise<void>;
}

export const PromptEditor: React.FC<PromptEditorProps> = ({
    chatId,
    title,
    prompt,
    isEditable,
    info,
    modificationHandler,
}) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const [value, setValue] = React.useState<string>(prompt);
    const { conversations } = useAppSelector((state) => state.conversations);

    React.useEffect(() => {
        // Taking a dependency on the chatId because the value state needs
        // to be reset when the chatId changes. Otherwise, the value may
        // not be updated when the user switches between chats that has
        // the same prompt.
        setValue(prompt);
    }, [chatId, prompt]);

    const onSaveButtonClick = () => {
        if (modificationHandler) {
            modificationHandler(value).catch((error) => {
                setValue(prompt);
                const message = `Error saving the new prompt: ${(error as Error).message}`;
                dispatch(
                    addAlert({
                        type: AlertType.Error,
                        message,
                    }),
                );
            });
        }
    };

    return (
        <div className={classes.root}>
            <div className={classes.horizontal}>
                <h3>{title}</h3>
                <Popover withArrow>
                    <PopoverTrigger disableButtonEnhancement>
                        <Button icon={<Info16 />} appearance="transparent" />
                    </PopoverTrigger>
                    <PopoverSurface className={classes.dialog}>{info}</PopoverSurface>
                </Popover>
            </div>
            <Textarea
                resize="vertical"
                value={value}
                rows={Rows}
                disabled={!isEditable}
                onChange={(_event, data) => {
                    setValue(data.value);
                }}
            />
            {isEditable && (
                <div className={classes.controls}>
                    <Button
                        onClick={onSaveButtonClick}
                        disabled={value.length <= 0 || value === prompt || conversations[chatId].disabled}
                    >
                        Save
                    </Button>
                </div>
            )}
        </div>
    );
};
