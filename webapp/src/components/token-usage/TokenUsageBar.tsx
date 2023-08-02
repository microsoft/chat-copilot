import { Popover, PopoverSurface, PopoverTrigger, tokens } from '@fluentui/react-components';
import { TokenUsageViewDetails } from '../../libs/models/TokenUsage';

interface ITokenUsageBar {
    details: TokenUsageViewDetails;
    totalUsage: number;
}

export const TokenUsageBar: React.FC<ITokenUsageBar> = ({ details, totalUsage }) => {
    const percentage = details.usageCount / totalUsage;
    const barWidth = percentage * 500;

    return (
        <Popover
            openOnHover
            mouseLeaveDelay={0}
            positioning={{
                position: 'above',
            }}
            withArrow
            appearance="inverted"
        >
            <PopoverTrigger>
                <div
                    key={details.legendLabel}
                    style={{
                        backgroundColor: details.color,
                        height: tokens.spacingVerticalMNudge,
                        width: `${barWidth}px`,
                    }}
                />
            </PopoverTrigger>
            <PopoverSurface>{`${details.legendLabel} (${details.usageCount})`}</PopoverSurface>
        </Popover>
    );
};
