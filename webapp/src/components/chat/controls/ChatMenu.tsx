import React from 'react';
import {
    Button,
    Menu,
    MenuItem,
    MenuList,
    MenuPopover,
    MenuTrigger,
    tokens,
    makeStyles,
} from '@fluentui/react-components';
import { ChevronDown16Regular } from '@fluentui/react-icons';
import { ISpecialization } from '../../../libs/models/Specialization';

interface ChatMenuProps {
    onNewChatClick: () => void;
    onDeleteChatHistory: () => void;
    botResponseStatus: string | undefined;
    chatSpecialization: ISpecialization | undefined;
}

const useClasses = makeStyles({
    menu: {
        backgroundColor: 'transparent',
        border: 'none',
        boxShadow: 'none',
        padding: '4px',
        width: 'auto',
        ':hover': {
            border: `0.5px solid ${tokens.colorNeutralStroke1Hover}`,
            color: 'black',
        },
        ':focus': {
            border: `0.5px solid ${tokens.colorNeutralStroke1Hover}`,
        },
        ':active': {
            border: `0.5px solid ${tokens.colorNeutralStroke1Pressed}`,
        },
    },
});

export const ChatMenu: React.FC<ChatMenuProps> = ({
    onNewChatClick,
    onDeleteChatHistory,
    botResponseStatus,
    chatSpecialization,
}) => {
    const classes = useClasses();

    return (
        <Menu>
            <MenuTrigger>
                <Button
                    appearance="transparent"
                    className={classes.menu}
                    icon={<ChevronDown16Regular />}
                    iconPosition="after"
                >
                    {chatSpecialization?.name}
                </Button>
            </MenuTrigger>
            <MenuPopover>
                <MenuList>
                    <MenuItem onClick={onNewChatClick} key="newChat">
                        New Chat
                    </MenuItem>
                    <MenuItem
                        onClick={onDeleteChatHistory}
                        key="deleteChatHistory"
                        disabled={botResponseStatus != null}
                    >
                        Clear Chat History
                    </MenuItem>
                </MenuList>
            </MenuPopover>
        </Menu>
    );
};
