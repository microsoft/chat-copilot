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
    divider: {
        width: '97%',
    },
});

interface ITokenUsageGraph {
    tokenUsage: TokenUsage;
    promptView?: boolean;
}

const contrastColors = [
    tokens.colorPaletteBlueBackground2,
    tokens.colorPaletteBlueForeground2,
    tokens.colorPaletteBlueBorderActive,
];

export const TokenUsageGraph: React.FC<ITokenUsageGraph> = ({ promptView, tokenUsage }) => {
    const classes = useClasses();
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const loadingResponse =
        selectedId !== '' && conversations[selectedId].botResponseStatus && Object.entries(tokenUsage).length === 0;

    const responseGenerationView: TokenUsageView = {};
    const memoryGenerationView: TokenUsageView = {};
    let memoryGenerationUsage = 0;
    let responseGenerationUsage = 0;

    const graphColors = {
        brand: {
            // Color index of semanticKernelBrandRamp array defined in styles.ts
            legend: 120 as Brands,
            index: 120 as Brands,
            getNextIndex: () => {
                const nextIndex = graphColors.brand.index - 20;
                return (nextIndex < 0 ? 160 : nextIndex) as Brands;
            },
        },
        contrast: {
            // Color index of contrastColors array defined above
            legend: 0,
            index: 0,
            getNextIndex: () => {
                return graphColors.contrast.index++ % 3;
            },
        },
    };

    Object.entries(tokenUsage).forEach(([key, value]) => {
        const viewDetails: TokenUsageViewDetails = {
            usageCount: value ?? 0,
            legendLabel: TokenUsageFunctionNameMap[key],
            color: semanticKernelBrandRamp[graphColors.brand.index],
        };

        if (key.toLocaleUpperCase().includes('MEMORY')) {
            memoryGenerationUsage += value ?? 0;
            viewDetails.color = contrastColors[graphColors.contrast.getNextIndex()];
            memoryGenerationView[key] = viewDetails;
        } else {
            responseGenerationUsage += value ?? 0;
            graphColors.brand.index = graphColors.brand.getNextIndex();
            responseGenerationView[key] = viewDetails;
        }
    });

    const totalUsage = memoryGenerationUsage + responseGenerationUsage;

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
                            see{' '}
                            <a href="https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai.completionsusage?view=azure-dotnet-preview">
                                CompletionsUsage
                            </a>{' '}
                            docs.
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
                                    {Object.entries(responseGenerationView).map(([key, details]) => {
                                        return <TokenUsageBar key={key} details={details} totalUsage={totalUsage} />;
                                    })}
                                    {Object.entries(memoryGenerationView).map(([key, details]) => {
                                        return <TokenUsageBar key={key} details={details} totalUsage={totalUsage} />;
                                    })}
                                </div>
                                <div className={mergeClasses(classes.legend, classes.horizontal)}>
                                    <TokenUsageLegendItem
                                        key={'Response Generation'}
                                        name={'Response Generation'}
                                        usageCount={responseGenerationUsage}
                                        items={Object.values(responseGenerationView)}
                                        color={semanticKernelBrandRamp[graphColors.brand.legend]}
                                    />
                                    <TokenUsageLegendItem
                                        key={'Memory Generation'}
                                        name={'Memory Generation'}
                                        usageCount={memoryGenerationUsage}
                                        items={Object.values(memoryGenerationView)}
                                        color={contrastColors[graphColors.contrast.legend]}
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
            <Divider className={classes.divider} />
        </>
    );
};
