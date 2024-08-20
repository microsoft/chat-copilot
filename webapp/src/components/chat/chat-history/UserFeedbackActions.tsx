import { useMsal } from '@azure/msal-react';
import { Button, Text, Tooltip, makeStyles } from '@fluentui/react-components';
import { useCallback } from 'react';
import { AuthHelper } from '../../../libs/auth/AuthHelper';
import { UserFeedback } from '../../../libs/models/ChatMessage';
import { ChatService } from '../../../libs/services/ChatService';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { updateMessageProperty } from '../../../redux/features/conversations/conversationsSlice';
import { ThumbDislike16, ThumbLike16 } from '../../shared/BundledIcons';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        'place-content': 'flex-end',
        alignItems: 'center',
    },
});

interface IUserFeedbackProps {
    messageId: string;
    wasHelpful?: string;
}

export const UserFeedbackActions: React.FC<IUserFeedbackProps> = ({ messageId, wasHelpful }: IUserFeedbackProps) => {
    const classes = useClasses();

    const { instance, inProgress } = useMsal();

    const dispatch = useAppDispatch();
    const { selectedId } = useAppSelector((state: RootState) => state.conversations);

    const onUserFeedbackProvided = useCallback(
        async (positive: boolean) => {
            const chatService = new ChatService();
            const userRating = positive ? UserFeedback.Positive : UserFeedback.Negative;
            const token = await AuthHelper.getSKaaSAccessToken(instance, inProgress);

            chatService
                .rateMessageAync(selectedId, messageId, positive, token)
                .then(() => {
                    dispatch(
                        updateMessageProperty({
                            chatId: selectedId,
                            messageIdOrIndex: messageId,
                            property: 'userFeedback',
                            value: userRating,
                            frontLoad: true,
                        }),
                    );
                })
                .catch((e) => {
                    console.error(e);
                });
        },
        [instance, inProgress, selectedId, messageId, dispatch],
    );

    return (
        <div className={classes.root}>
            <Text color="gray" size={200}>
                Was this response helpful?
            </Text>
            <Tooltip content={'Like'} relationship="label">
                <Button
                    icon={<ThumbLike16 filled={wasHelpful === UserFeedback.Positive} />}
                    appearance="transparent"
                    aria-label="Edit"
                    onClick={() => {
                        void onUserFeedbackProvided(true);
                    }}
                />
            </Tooltip>
            <Tooltip content={'Dislike'} relationship="label">
                <Button
                    icon={<ThumbDislike16 filled={wasHelpful === UserFeedback.Negative} />}
                    appearance="transparent"
                    aria-label="Edit"
                    onClick={() => {
                        void onUserFeedbackProvided(false);
                    }}
                />
            </Tooltip>
        </div>
    );
};
