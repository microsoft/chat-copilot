/* eslint-disable @typescript-eslint/no-unsafe-call */
/* eslint-disable react/jsx-key */
import { makeStyles, shorthands, Text, tokens } from '@fluentui/react-components';
import { RootState } from '../../../../redux/app/store';
import { Breakpoints } from '../../../../styles';
import { SpecializationListItem } from './SpecializationListItem';
import { useAppSelector } from '../../../../redux/app/hooks';

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
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-return
    const { specializations } = useAppSelector((state: RootState) => state.admin);
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-return
    const { selectedKey } = useAppSelector((state: RootState) => state.admin);

    return specializations.length > 0 ? (
        <div className={classes.root}>
            <Text className={classes.header}>{header}</Text>
            {specializations.map((specialization) => {
                const isSelected = specialization.key === selectedKey;
                return specialization.key != 'general' ? (
                    <SpecializationListItem
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
