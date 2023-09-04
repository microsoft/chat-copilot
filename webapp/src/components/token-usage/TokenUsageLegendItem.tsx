import {
    Popover,
    PopoverSurface,
    PopoverTrigger,
    Text,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { TokenUsageViewDetails } from '../../libs/models/TokenUsage';
import { TokenUsageLegendLabel } from './TokenUsageLegendLabel';

export const useClasses = makeStyles({
    root: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
        alignItems: 'center',
    },
    colors: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalXXS),
    },
    legendColor: {
        height: tokens.spacingVerticalMNudge,
        width: tokens.spacingHorizontalMNudge,
    },
});

interface ITokenUsageLegendItem {
    name: string;
    usageCount: number;
    items: TokenUsageViewDetails[];
    color: string;
}

export const TokenUsageLegendItem: React.FC<ITokenUsageLegendItem> = ({ name, usageCount, items, color }) => {
    const classes = useClasses();
    return (
        <div className={classes.root}>
            <Popover
                openOnHover
                mouseLeaveDelay={0}
                positioning={{
                    position: 'below',
                    align: 'start',
                }}
                withArrow
                appearance="inverted"
            >
                <PopoverTrigger>
                    <div
                        key={color}
                        style={{
                            backgroundColor: color,
                            height: tokens.spacingVerticalMNudge,
                            width: tokens.spacingHorizontalMNudge,
                        }}
                    />
                </PopoverTrigger>
                <PopoverSurface>
                    {items.length > 0
                        ? items.map((details) => {
                              return <TokenUsageLegendLabel key={details.legendLabel} details={details} />;
                          })
                        : 'No usage'}
                    {
                        // TODO: [Issue #150, sk#2106] Remove this once core team finishes work to return token usage.
                        name === 'Response Generation' && `(Planner usage coming soon)`
                    }
                </PopoverSurface>
            </Popover>
            <Text>{`${name} (${usageCount})`}</Text>
        </div>
    );
};
