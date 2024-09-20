import { Button, Tooltip } from '@fluentui/react-components';
import { ChatDismissRegular } from '@fluentui/react-icons';
import { FC } from 'react';
import { useChat } from '../../../libs/hooks';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';

interface DeleteChatHistoryProps {
    chatId: string;
}

/**
 * A React functional component for deleting chat history.
 *
 * @param {DeleteChatHistoryProps} props - The properties passed to the component.
 * @param {string} props.chatId - The unique identifier for the chat to be deleted.
 *
 * @component
 * @returns {JSX.Element} A button wrapped with a tooltip for deleting chat history.
 */
export const DeleteChatHistory: FC<DeleteChatHistoryProps> = ({ chatId }) => {
    const chat = useChat();

    const { conversations } = useAppSelector((state: RootState) => state.conversations);
    const botResponseStatus = conversations[chatId].botResponseStatus;

    const onDeleteChatHistory = () => {
        void chat.deleteChatHistory(chatId);
    };

    return (
        <div>
            <Tooltip content={'Clear chat history'} relationship="label">
                <Button
                    onClick={onDeleteChatHistory}
                    icon={<ChatDismissRegular />}
                    appearance="transparent"
                    disabled={botResponseStatus != null}
                />
            </Tooltip>
        </div>
    );
};
