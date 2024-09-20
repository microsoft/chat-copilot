import {
    makeStyles,
    SelectTabEventHandler,
    shorthands,
    Tab,
    TabList,
    TabValue,
    tokens,
} from '@fluentui/react-components';
import React, { FC, useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { setAdminSelected } from '../../../redux/features/admin/adminSlice';
import { setSearchSelected } from '../../../redux/features/search/searchSlice';
import { Breakpoints } from '../../../styles';
import { SpecializationList } from '../../admin/specialization/specialization-list/SpecializationList';
import { SearchList } from '../../search/search-list/SearchList';
import { ChatList } from './ChatList';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexShrink: 0,
        width: '320px',
        backgroundColor: tokens.colorNeutralBackground4,
        flexDirection: 'column',
        ...shorthands.overflow('hidden'),
        ...Breakpoints.small({
            width: '64px',
        }),
    },
});

export const ChatType: FC = () => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const activeUserInfo = useAppSelector((state: RootState) => state.app.activeUserInfo);
    const [selectedTab, setSelectedTab] = React.useState<TabValue>('chat');
    const [hasAdmin, setHasAdmin] = useState(false);
    const onTabSelect: SelectTabEventHandler = (_event, data) => {
        setSelectedTab(data.value);
    };
    useEffect(() => {
        if (activeUserInfo) {
            setHasAdmin(activeUserInfo.hasAdmin);
        }
    }, [activeUserInfo]);

    useEffect(() => {
        if (selectedTab === 'search') {
            const selectedConversation = conversations[selectedId];
            if (selectedConversation.specializationId) {
                const chatSpecializationId = selectedConversation.specializationId;
                void dispatch(setSearchSelected({ selected: true, specializationId: chatSpecializationId }));
            } else {
                dispatch(setSearchSelected({ selected: true, specializationId: '' }));
            }
            dispatch(setAdminSelected(false));
        } else if (selectedTab === 'admin') {
            dispatch(setAdminSelected(true));
            dispatch(setSearchSelected({ selected: false, specializationId: '' }));
        } else {
            dispatch(setSearchSelected({ selected: false, specializationId: '' }));
            dispatch(setAdminSelected(false));
        }
    }, [selectedTab, conversations, selectedId, dispatch]);

    return (
        <div className={classes.root}>
            <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
                <Tab data-testid="chatTab" id="chat" value="chat" aria-label="Chat Tab" title="Chat Tab">
                    Chat
                </Tab>
                <Tab data-testid="searchTab" id="search" value="search" aria-label="Search Tab" title="Search Tab">
                    Search - Beta
                </Tab>
                {hasAdmin && (
                    <Tab data-testid="adminTab" id="admin" value="admin" aria-label="admin Tab" title="Admin Tab">
                        Admin
                    </Tab>
                )}
            </TabList>
            {selectedTab === 'chat' && <ChatList />}
            {selectedTab === 'search' && <SearchList />}
            {selectedTab === 'admin' && <SpecializationList />}
        </div>
    );
};
