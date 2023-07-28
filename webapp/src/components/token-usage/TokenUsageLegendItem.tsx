import {
    Popover,
    PopoverSurface,
    PopoverTrigger,
    Text,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { TokenUsageView } from '../../libs/models/TokenUsage';
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
    items: TokenUsageView;
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
                    {Object.values(items).map((details) => {
                        return <TokenUsageLegendLabel key={details.legendLabel} details={details} />;
                    })}
                </PopoverSurface>
            </Popover>
            <Text>{`${name} (${usageCount})`}</Text>
        </div>
    );
};
