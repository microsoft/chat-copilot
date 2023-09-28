import { Button } from '@fluentui/react-button';
import { makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { Input, InputOnChangeData } from '@fluentui/react-input';
import { useEffect, useState } from 'react';
import { useChat } from '../../../libs/hooks';
import { AlertType } from '../../../libs/models/AlertType';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { addAlert } from '../../../redux/features/app/appSlice';
import { editConversationTitle } from '../../../redux/features/conversations/conversationsSlice';
import { Breakpoints } from '../../../styles';
import { Checkmark20, Dismiss20 } from '../../shared/BundledIcons';
import { getErrorDetails } from '../../utils/TextUtils';

const useClasses = makeStyles({
    root: {
        width: '100%',
        ...Breakpoints.small({
            display: 'none',
        }),
    },
    buttons: {
        display: 'flex',
        alignSelf: 'end',
    },
    textButtons: {
        ...shorthands.gap(tokens.spacingVerticalS),
    },
});

interface IEditChatNameProps {
    name: string;
    chatId: string;
    exitEdits: () => void;
    textButtons?: boolean;
}

export const EditChatName: React.FC<IEditChatNameProps> = ({ name, chatId, exitEdits, textButtons }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const chat = useChat();

    const [title = '', setTitle] = useState<string | undefined>(name);

    useEffect(() => {
        if (selectedId !== chatId) exitEdits();
    }, [chatId, exitEdits, selectedId]);

    const onSaveTitleChange = async () => {
        if (name !== title) {
            const chatState = conversations[selectedId];
            await chat.editChat(chatId, title, chatState.systemDescription, chatState.memoryBalance).then(() => {
                dispatch(editConversationTitle({ id: chatId, newTitle: title }));
            });
        }
        exitEdits();
    };

    const onClose = () => {
        setTitle(name);
        exitEdits();
    };

    const onTitleChange = (_ev: React.ChangeEvent<HTMLInputElement>, data: InputOnChangeData) => {
        setTitle(data.value);
    };

    const handleSave = () => {
        onSaveTitleChange().catch((e: any) => {
            const errorMessage = `Unable to retrieve chat to change title. Details: ${getErrorDetails(e)}`;
            dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
        });
    };

    const handleKeyDown: React.KeyboardEventHandler<HTMLElement> = (event) => {
        if (event.key === 'Enter') {
            handleSave();
        }
    };

    return (
        <div
            className={classes.root}
            style={{
                display: 'flex',
                flexDirection: `${textButtons ? 'column' : 'row'}`,
                gap: `${textButtons ? tokens.spacingVerticalS : tokens.spacingVerticalNone}`,
            }}
            title={'Edit chat name'}
            aria-label={`Edit chat name for "${name}"`}
        >
            <Input value={title} onChange={onTitleChange} id={`input-${chatId}`} onKeyDown={handleKeyDown} autoFocus />
            {textButtons && (
                <div className={mergeClasses(classes.buttons, classes.textButtons)}>
                    <Button appearance="secondary" onClick={onClose}>
                        Cancel
                    </Button>
                    <Button type="submit" appearance="primary" onClick={handleSave}>
                        Save
                    </Button>
                </div>
            )}
            {!textButtons && (
                <div className={classes.buttons}>
                    <Button icon={<Dismiss20 />} onClick={onClose} appearance="transparent" />
                    <Button icon={<Checkmark20 />} onClick={handleSave} appearance="transparent" />
                </div>
            )}
        </div>
    );
};
