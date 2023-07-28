import { Popover, PopoverSurface, PopoverTrigger } from '@fluentui/react-components';
import { Constants } from '../../Constants';

interface ITokenUsageBar {
    functionName: string;
    functionUsage: number;
    totalUsage: number;
    color: string;
}

export const TokenUsageBar: React.FC<ITokenUsageBar> = ({ functionName, functionUsage, totalUsage, color }) => {
    const percentage = functionUsage / totalUsage;
    const barWidth = percentage * Constants.MAX_BAR_GRAPH_WIDTH;

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
                    key={functionName}
                    style={{
                        backgroundColor: color,
                        height: '10px',
                        width: `${barWidth}px`,
                    }}
                />
            </PopoverTrigger>
            <PopoverSurface>{`${functionName} (${functionUsage})`}</PopoverSurface>
        </Popover>
    );
};
