import { Text, tokens } from '@fluentui/react-components';
import { TokenUsageViewDetails } from '../../libs/models/TokenUsage';
import { useClasses } from './TokenUsageLegendItem';

interface ITokenUsageLegendLabel {
    details: TokenUsageViewDetails;
}

export const TokenUsageLegendLabel: React.FC<ITokenUsageLegendLabel> = ({ details }) => {
    const classes = useClasses();
    return (
        <div className={classes.root}>
            <div
                style={{
                    backgroundColor: details.color,
                    height: tokens.spacingVerticalMNudge,
                    width: tokens.spacingHorizontalMNudge,
                }}
            />
            <Text>{`${details.legendLabel} (${details.usageCount})`}</Text>
        </div>
    );
};
