/* eslint-disable @typescript-eslint/no-unsafe-call */
/* eslint-disable react/jsx-key */
import { makeStyles, shorthands, Text, tokens } from '@fluentui/react-components';
import { useAppSelector } from '../../../../redux/app/hooks';
import { RootState } from '../../../../redux/app/store';
import { Breakpoints } from '../../../../styles';
import { SpecializationListItem } from './SpecializationListItem';

const useClasses = makeStyles({
    root: {
        display: 'block',
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
}

export const SpecializationListSection: React.FC<IChatListSectionProps> = ({ header }) => {
    const classes = useClasses();
    const specializations = useAppSelector((state: RootState) => state.admin.specializations);
    const selectedKey = useAppSelector((state: RootState) => state.admin.selectedKey);

    return specializations.length > 0 ? (
        <div className={classes.root}>
            <Text className={classes.header}>{header}</Text>
            {specializations.map((specialization) => {
                const isSelected = specialization.key === selectedKey;
                return specialization.key != 'general' ? (
                    <SpecializationListItem
                        key={specialization.id}
                        specializationId={specialization.id}
                        specializationKey={specialization.key}
                        name={specialization.name}
                        specializationMode={specialization.isActive}
                        isSelected={isSelected}
                    />
                ) : null;
            })}
        </div>
    ) : null;
};
