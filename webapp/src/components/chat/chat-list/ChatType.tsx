import {
    makeStyles,
    shorthands,
    tokens,
    Tab,
    TabList,
    TabValue,
    SelectTabEventHandler,
} from '@fluentui/react-components';
import React, { FC, useEffect } from 'react';
import { Breakpoints } from '../../../styles';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { ChatList } from './ChatList';
import { SearchList } from '../../search/search-list/SearchList';
import { setSearchSelected } from '../../../redux/features/search/searchSlice';
import { RootState } from '../../../redux/app/store';

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
    const [selectedTab, setSelectedTab] = React.useState<TabValue>('chat');
    const onTabSelect: SelectTabEventHandler = (_event, data) => {
        setSelectedTab(data.value);
    };

    useEffect(() => {
        if (selectedTab === 'search') {
            const chatSpecializationKey: string = conversations[selectedId].specializationKey || '';
            dispatch(setSearchSelected({ selected: true, specializationKey: chatSpecializationKey }));
        } else {
            dispatch(setSearchSelected({ selected: false, specializationKey: '' }));
        }
    }, [selectedTab, conversations, selectedId, dispatch]);

    return (
        <div className={classes.root}>
            <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
                <Tab data-testid="chatTab" id="chat" value="chat" aria-label="Chat Tab" title="Chat Tab">
                    Chat
                </Tab>
                <Tab data-testid="searchTab" id="search" value="search" aria-label="Search Tab" title="Search Tab">
                    Search
                </Tab>
            </TabList>
            {selectedTab === 'chat' && <ChatList />}
            {selectedTab === 'search' && <SearchList />}
        </div>
    );
};
