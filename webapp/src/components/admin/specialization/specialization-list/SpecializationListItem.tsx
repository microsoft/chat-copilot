import { makeStyles, mergeClasses, shorthands, Text, tokens } from '@fluentui/react-components';

import { FC, useId } from 'react';
import { useAppDispatch } from '../../../../redux/app/hooks';
import { setSelectedKey } from '../../../../redux/features/admin/adminSlice';
import { Breakpoints, SharedStyles } from '../../../../styles';
import { SpecializationListItemActions } from '../SpecializationListItemActions';

const useClasses = makeStyles({
    root: {
        boxSizing: 'border-box',
        display: 'inline-flex',
        flexDirection: 'row',
        width: '100%',
        ...Breakpoints.small({
            justifyContent: 'center',
        }),
        cursor: 'pointer',
        ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalXL),
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
    previewText: {
        ...SharedStyles.overflowEllipsis,
        display: 'flex',
        lineHeight: tokens.lineHeightBase100,
        color: tokens.colorNeutralForeground2,
    },
    selected: {
        backgroundColor: tokens.colorNeutralBackground1,
    },
    l1: {
        width: '30px',
    },
    specialization: {
        fontStyle: 'italic',
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground2,
    },
});

interface ISpecializationListItemProps {
    specializationId: string;
    label: string;
    name: string;
    specializationMode: boolean;
    isSelected: boolean;
}

export const SpecializationListItem: FC<ISpecializationListItemProps> = ({
    specializationId,
    label,
    name,
    specializationMode,
    isSelected,
}) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const friendlyTitle = name.length > 30 ? name.substring(0, 30) + '...' : name;
    const onEditSpecializationClick = (specializationId: string) => {
        dispatch(setSelectedKey(specializationId));
    };

    return (
        <div
            className={mergeClasses(classes.root, isSelected && classes.selected)}
            onClick={() => {
                onEditSpecializationClick(specializationId);
            }}
            title={`Chat: ${friendlyTitle}`}
            aria-label={`Chat list item: ${friendlyTitle}`}
        >
            <>
                <div key={useId()} className={classes.body}>
                    <div className={classes.header}>
                        <Text className={classes.title} title={friendlyTitle}>
                            {friendlyTitle}
                        </Text>
                    </div>
                    <Text className={classes.specialization} title={label}>
                        {label}
                    </Text>
                </div>
                <SpecializationListItemActions
                    specializationId={specializationId}
                    specializationMode={specializationMode}
                />
            </>
        </div>
    );
};
