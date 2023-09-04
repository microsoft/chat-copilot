import { makeStyles, mergeClasses, Persona, shorthands, Text, tokens } from '@fluentui/react-components';
import { ShieldTask16Regular } from '@fluentui/react-icons';
import { FC, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { FeatureKeys } from '../../../redux/features/app/AppState';
import { setSelectedConversation } from '../../../redux/features/conversations/conversationsSlice';
import { Breakpoints, SharedStyles } from '../../../styles';
import { timestampToDateString } from '../../utils/TextUtils';
import { EditChatName } from '../shared/EditChatName';
import { ListItemActions } from './ListItemActions';

const useClasses = makeStyles({
    root: {
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'row',
        width: '100%',
        ...Breakpoints.small({
            justifyContent: 'center',
        }),
        cursor: 'pointer',
        ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalXL),
    },
    avatar: {
        flexShrink: 0,
        width: '32px',
    },
    body: {
        minWidth: 0,
        width: '100%',
        display: 'flex',
        flexDirection: 'column',
        marginLeft: tokens.spacingHorizontalXS,
        ...Breakpoints.small({
            display: 'none',
        }),
        alignSelf: 'center',
    },
    header: {
        flexGrow: 1,
        display: 'flex',
        flexDirection: 'row',
        justifyContent: 'space-between',
    },
    title: {
        ...SharedStyles.overflowEllipsis,
        fontSize: tokens.fontSizeBase300,
        color: tokens.colorNeutralForeground1,
    },
    timestamp: {
        flexShrink: 0,
        marginLeft: tokens.spacingHorizontalM,
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground2,
        lineHeight: tokens.lineHeightBase200,
    },
    previewText: {
        ...SharedStyles.overflowEllipsis,
        display: 'block',
        lineHeight: tokens.lineHeightBase100,
        color: tokens.colorNeutralForeground2,
    },
    popoverSurface: {
        display: 'none',
        ...Breakpoints.small({
            display: 'flex',
            flexDirection: 'column',
        }),
    },
    selected: {
        backgroundColor: tokens.colorNeutralBackground1,
    },
    protectedIcon: {
        color: tokens.colorPaletteLightGreenBorder1,
        verticalAlign: 'text-bottom',
        marginLeft: tokens.spacingHorizontalXS,
    },
});

interface IChatListItemProps {
    id: string;
    header: string;
    timestamp: number;
    preview: string;
    botProfilePicture: string;
    isSelected: boolean;
}

export const ChatListItem: FC<IChatListItemProps> = ({
    id,
    header,
    timestamp,
    preview,
    botProfilePicture,
    isSelected,
}) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { features } = useAppSelector((state: RootState) => state.app);

    const showPreview = !features[FeatureKeys.SimplifiedExperience].enabled && preview;
    const showActions = features[FeatureKeys.SimplifiedExperience].enabled && isSelected;

    const [editingTitle, setEditingTitle] = useState(false);

    const onClick = (_ev: any) => {
        dispatch(setSelectedConversation(id));
    };

    const time = timestampToDateString(timestamp);
    return (
        <div
            className={mergeClasses(classes.root, isSelected && classes.selected)}
            onClick={onClick}
            title={`Chat: ${header}`}
            aria-label={`Chat list item: ${header}`}
        >
            <Persona
                avatar={{ image: { src: botProfilePicture } }}
                presence={!features[FeatureKeys.SimplifiedExperience].enabled ? { status: 'available' } : undefined}
            />
            {editingTitle ? (
                <EditChatName
                    name={header}
                    chatId={id}
                    exitEdits={() => {
                        setEditingTitle(false);
                    }}
                />
            ) : (
                <>
                    <div className={classes.body}>
                        <div className={classes.header}>
                            <Text className={classes.title} title={header}>
                                {header}
                                {features[FeatureKeys.AzureContentSafety].enabled && (
                                    <ShieldTask16Regular className={classes.protectedIcon} />
                                )}
                            </Text>
                            {!features[FeatureKeys.SimplifiedExperience].enabled && (
                                <Text className={classes.timestamp} size={300}>
                                    {time}
                                </Text>
                            )}
                        </div>
                        {showPreview && (
                            <>
                                {
                                    <Text id={`message-preview-${id}`} size={200} className={classes.previewText}>
                                        {preview}
                                    </Text>
                                }
                            </>
                        )}
                    </div>
                    {showActions && (
                        <ListItemActions
                            chatId={id}
                            onEditTitleClick={() => {
                                setEditingTitle(true);
                            }}
                        />
                    )}
                </>
            )}
        </div>
    );
};
