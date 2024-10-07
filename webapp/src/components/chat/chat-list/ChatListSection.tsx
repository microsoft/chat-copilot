import { useMsal } from '@azure/msal-react';
import { makeStyles, shorthands, Text, tokens } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import { AuthHelper } from '../../../libs/auth/AuthHelper';
import { getFriendlyChatName } from '../../../libs/hooks/useChat';
import { ChatMessageType } from '../../../libs/models/ChatMessage';
import { ISpecialization } from '../../../libs/models/Specialization';
import { SpecializationService } from '../../../libs/services/SpecializationService';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { Conversations } from '../../../redux/features/conversations/ConversationsState';
import { Breakpoints } from '../../../styles';
import { ChatListItem } from './ChatListItem';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalXXS),
        paddingBottom: tokens.spacingVerticalXS,
    },
    header: {
        marginTop: 0,
        paddingBottom: tokens.spacingVerticalXS,
        marginLeft: tokens.spacingHorizontalXL,
        marginRight: tokens.spacingHorizontalXL,
        fontWeight: tokens.fontWeightRegular,
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3,
        ...Breakpoints.small({
            display: 'none',
        }),
    },
});

interface IChatListSectionProps {
    header?: string;
    conversations: Conversations;
}

export const ChatListSection: React.FC<IChatListSectionProps> = ({ header, conversations }) => {
    const classes = useClasses();
    const { selectedId } = useAppSelector((state: RootState) => state.conversations);
    const keys = Object.keys(conversations);
    const { instance, inProgress } = useMsal();
    const specializationService = new SpecializationService();

    const [specializationMap, setSpecializationMap] = useState(new Map<string, ISpecialization>());
    useEffect(() => {
        async function loadSpecializations() {
            const map = new Map<string, ISpecialization>();

            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            const specializations = await specializationService.getAllSpecializationsAsync(accessToken);
            for (const specialization of specializations) {
                map.set(specialization.id, specialization);
            }
            setSpecializationMap(map);
        }
        void loadSpecializations();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return keys.length > 0 ? (
        <div className={classes.root}>
            <Text className={classes.header}>{header}</Text>
            {keys.map((id) => {
                const convo = conversations[id];
                const messages = convo.messages;
                const lastMessage = messages[convo.messages.length - 1];
                const isSelected = id === selectedId;
                const specialization = !!convo.specializationId
                    ? specializationMap.get(convo.specializationId)
                    : undefined;
                return (
                    <ChatListItem
                        id={id}
                        key={id}
                        isSelected={isSelected}
                        header={getFriendlyChatName(convo)}
                        timestamp={convo.lastUpdatedTimestamp ?? (messages.length > 0 ? lastMessage.timestamp : 0)} //Note: using 0 as a default unix epoch time value here
                        preview={
                            messages.length > 0
                                ? lastMessage.type === ChatMessageType.Document
                                    ? 'Sent a file'
                                    : lastMessage.type === ChatMessageType.Plan
                                      ? 'Click to view proposed plan'
                                      : lastMessage.content
                                : 'Click to start the chat'
                        }
                        botProfilePicture={
                            specialization?.iconFilePath ? specialization.iconFilePath : convo.botProfilePicture
                        }
                        specializationLabel={specialization?.label ?? ''}
                    />
                );
            })}
        </div>
    ) : null;
};
