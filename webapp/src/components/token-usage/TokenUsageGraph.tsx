import {
    Body1,
    Button,
    Divider,
    makeStyles,
    mergeClasses,
    Popover,
    PopoverSurface,
    PopoverTrigger,
    shorthands,
    Text,
    tokens,
} from '@fluentui/react-components';
import { Brands } from '@fluentui/tokens';
import {
    TokenUsage,
    TokenUsageFunctionNameMap,
    TokenUsageView,
    TokenUsageViewDetails,
} from '../../libs/models/TokenUsage';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { semanticKernelBrandRamp } from '../../styles';
import { TypingIndicator } from '../chat/typing-indicator/TypingIndicator';
import { Info16 } from '../shared/BundledIcons';
import { TokenUsageBar } from './TokenUsageBar';
import { TokenUsageLegendItem } from './TokenUsageLegendItem';

const useClasses = makeStyles({
    horizontal: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
        alignItems: 'center',
    },
    content: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingHorizontalS),
        paddingBottom: tokens.spacingHorizontalM,
    },
    popover: {
        width: '300px',
    },
    header: {
        marginBlockEnd: tokens.spacingHorizontalM,
    },
    legend: {
        'flex-flow': 'wrap',
    },
});

interface ITokenUsageGraph {
    tokenUsage: TokenUsage;
    promptView?: boolean;
}

export const TokenUsageGraph: React.FC<ITokenUsageGraph> = ({ promptView, tokenUsage }) => {
    const classes = useClasses();
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const loadingResponse = conversations[selectedId].botResponseStatus;

    const tokenUsageView: TokenUsageView = {};
    let dependencyUsage = 0;
    let responseGenerationUsage = 0;
    let color = 160 as Brands;
    Object.entries(tokenUsage).forEach(([key, value]) => {
        const viewDetails: TokenUsageViewDetails = {
            usageCount: value ?? 0,
            legendLabel: TokenUsageFunctionNameMap[key],
            dependency: false,
            color: semanticKernelBrandRamp[color],
        };

        // Iterate through the color ramp
        color -= 20;

        if (key === 'responseCompletion' || key == 'metaPromptTemplate') {
            responseGenerationUsage += value ?? 0;
            viewDetails.dependency = false;
        } else {
            dependencyUsage += value ?? 0;
            viewDetails.dependency = true;
        }
        tokenUsageView[key] = viewDetails;
    });

    const totalUsage = dependencyUsage + responseGenerationUsage;

    return (
        <>
            <h3 className={classes.header}>
                Token Usage
                <Popover withArrow>
                    <PopoverTrigger disableButtonEnhancement>
                        <Button icon={<Info16 />} appearance="transparent" />
                    </PopoverTrigger>
                    <PopoverSurface className={classes.popover}>
                        <Body1>
                            Token count for each category is the total sum of tokens used for the prompt template and
                            chat completion for the respective completion functions. For more details about token usage,
                            see:{' '}
                            <a href="https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai.completionsusage?view=azure-dotnet-preview">
                                CompletionsUsage docs here.
                            </a>
                        </Body1>
                    </PopoverSurface>
                </Popover>
            </h3>
            <div className={classes.content}>
                {loadingResponse ? (
                    <Body1>
                        Final token usage will be available once bot response is generated.
                        <TypingIndicator />
                    </Body1>
                ) : (
                    <>
                        {totalUsage > 0 ? (
                            <>
                                {!promptView && <Text>Total token usage for current session</Text>}
                                <div className={classes.horizontal} style={{ gap: tokens.spacingHorizontalXXS }}>
                                    {Object.entries(tokenUsageView).map(([key, details]) => {
                                        return (
                                            <TokenUsageBar
                                                key={key}
                                                functionName={details.legendLabel}
                                                functionUsage={details.usageCount}
                                                totalUsage={totalUsage}
                                                color={details.color}
                                            />
                                        );
                                    })}
                                </div>
                                <div className={mergeClasses(classes.legend, classes.horizontal)}>
                                    <TokenUsageLegendItem
                                        key={'Dependencies'}
                                        name={'Dependencies'}
                                        usageCount={dependencyUsage}
                                        items={Object.values(tokenUsageView).filter((x) => x.dependency)}
                                    />
                                    <TokenUsageLegendItem
                                        key={'Response Generation'}
                                        name={'Response Generation'}
                                        usageCount={responseGenerationUsage}
                                        items={Object.values(tokenUsageView).filter((x) => !x.dependency)}
                                    />
                                </div>
                            </>
                        ) : promptView ? (
                            <Text>No tokens were used. This is a hardcoded response.</Text>
                        ) : (
                            <Text>No tokens have been used in this session yet.</Text>
                        )}
                    </>
                )}
            </div>
            <Divider />
        </>
    );
};
