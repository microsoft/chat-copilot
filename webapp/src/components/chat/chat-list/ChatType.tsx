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
import { useAppDispatch } from '../../../redux/app/hooks';
import { ChatList } from './ChatList';
import { SearchList } from '../../search/search-list/SearchList';
import {  setSearchSelected } from '../../../redux/features/search/searchSlice';
// import { searchData } from '../../../assets/const';

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
    }
});

export const ChatType: FC = () => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const [selectedTab, setSelectedTab] = React.useState<TabValue>('chat');
    const onTabSelect: SelectTabEventHandler = (_event, data) => {
        setSelectedTab(data.value);
    };

    useEffect(() => {
        selectedTab === 'search' ? dispatch(setSearchSelected(true)) : dispatch(setSearchSelected(false));
        // selectedTab === 'search' ? dispatch(setSearch(searchData)) : null
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedTab]);

    return (
        <div className={classes.root}>
            <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
                <Tab data-testid="chatTab" id="chat" value="chat" aria-label="Chat Tab" title="Chat Tab">
                    Chat
                </Tab>
                <Tab
                    data-testid="searchTab"
                    id="search"
                    value="search"
                    aria-label="Search Tab"
                    title="Search Tab"
                >
                    Search
                </Tab>
            </TabList>
            {selectedTab === 'chat' && <ChatList />}
            {selectedTab === 'search' && <SearchList />}
            
        </div>
    );
};
