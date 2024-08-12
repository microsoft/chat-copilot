import { makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';

import { FC } from 'react';
import { useAppDispatch } from '../../../redux/app/hooks';
import { Breakpoints, SharedStyles } from '../../../styles';
import { setSelectedSearchItem } from '../../../redux/features/search/searchSlice';
import { useId } from 'react';

const useClasses = makeStyles({
    root: {
        boxSizing: 'border-box',
        display: 'flex',
        flexDirection: 'row',
        // width: '100%',
        ...Breakpoints.small({
            justifyContent: 'center',
        }),
        cursor: 'pointer',
        ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalXL),
        marginLeft: tokens.spacingHorizontalM,
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

interface ISearchListItemProps {
    label: string;
    id: string;
    isSelected: boolean;
}

export const SearchListItem: FC<ISearchListItemProps> = ({ label, id, isSelected }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();

    const onClick = (_ev: any) => {
        dispatch(setSelectedSearchItem(id));
    };

    return (
        <div
            key={useId()}
            className={mergeClasses(classes.root, isSelected && classes.selected)}
            onClick={onClick}
            title={label}
            aria-label={label}
        >
            {label}
        </div>
    );
};
