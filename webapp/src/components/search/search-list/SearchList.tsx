// Copyright (c) Microsoft. All rights reserved.

import {
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { FC } from 'react';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { Breakpoints } from '../../../styles';
import { SearchListSection } from '../SearchListSection';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexShrink: 0,
        width: '320px',
        backgroundColor: tokens.colorNeutralBackground4,
        flexDirection: 'column',
        ...Breakpoints.small({
            width: '64px',
        }),
    },
    list: {
        overflowY: 'auto',
        '&:hover': {
            '&::-webkit-scrollbar-thumb': {
                backgroundColor: tokens.colorScrollbarOverlay,
                visibility: 'visible',
            },
        },
        '&::-webkit-scrollbar-track': {
            backgroundColor: tokens.colorSubtleBackground,
        },
        alignItems: 'stretch',
    },
    header: {
        display: 'flex',
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginRight: tokens.spacingVerticalM,
        marginLeft: tokens.spacingHorizontalXL,
        alignItems: 'center',
        height: '60px',
        ...Breakpoints.small({
            justifyContent: 'center',
        }),
    },
    title: {
        flexGrow: 1,
        ...Breakpoints.small({
            display: 'none',
        }),
        fontSize: tokens.fontSizeBase500,
    },
    input: {
        flexGrow: 1,
        ...shorthands.padding(tokens.spacingHorizontalNone),
        ...shorthands.border(tokens.borderRadiusNone),
        backgroundColor: tokens.colorSubtleBackground,
        fontSize: tokens.fontSizeBase500,
    },
});

export const SearchList: FC = () => {
    const classes = useClasses();
    const { searchData } = useAppSelector((state: RootState) => state.search);
    const values = searchData.value 
    return (
        <div className={classes.root}>
            <div aria-label={'chat list'} className={classes.list}>
                {values.map((value, index) => {
                    return(<SearchListSection
                                index={index}
                                value={value}
                            />)
                })}
            </div>
        </div>
    );
};
