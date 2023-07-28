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

const useClasses = makeStyles({
    root: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
        alignItems: 'center',
    },
    colors: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalXXS),
    },
});

interface ITokenUsageLegendItem {
    name: string;
    usageCount: number;
    items: TokenUsageViewDetails[];
}

export const TokenUsageLegendItem: React.FC<ITokenUsageLegendItem> = ({ name, usageCount, items }) => {
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
                    <div className={classes.colors}>
                        {items.map((item) => {
                            return (
                                <div
                                    key={item.color}
                                    style={{
                                        backgroundColor: item.color,
                                        height: tokens.spacingVerticalMNudge,
                                        width: tokens.spacingHorizontalMNudge,
                                    }}
                                />
                            );
                        })}
                    </div>
                </PopoverTrigger>
                <PopoverSurface>
                    {items.map((item) => {
                        return (
                            <div key={item.legendLabel} className={classes.root}>
                                <div
                                    style={{
                                        backgroundColor: item.color,
                                        height: tokens.spacingVerticalMNudge,
                                        width: tokens.spacingHorizontalMNudge,
                                    }}
                                />
                                <Text>{`${item.legendLabel} (${item.usageCount})`}</Text>
                            </div>
                        );
                    })}
                </PopoverSurface>
            </Popover>
            <Text>{`${name} (${usageCount})`}</Text>
        </div>
    );
};
