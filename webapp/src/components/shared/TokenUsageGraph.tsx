import {
    Body1,
    Body1Strong,
    Body2,
    Button,
    Divider,
    makeStyles,
    Popover,
    PopoverSurface,
    PopoverTrigger,
    shorthands,
    Text,
    tokens,
} from '@fluentui/react-components';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { TypingIndicator } from '../chat/typing-indicator/TypingIndicator';
import { Info16 } from './BundledIcons';

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
});

interface ITokenUsageGraph {
    promptUsage?: number;
    dependencyUsage?: number;
    promptView?: boolean;
}

export const TokenUsageGraph: React.FC<ITokenUsageGraph> = ({ promptView, promptUsage, dependencyUsage }) => {
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);

    const hasTokenCount = (promptUsage ?? dependencyUsage) !== undefined;
    const loadingResponse = conversations[selectedId].botResponseStatus;

    // Necessary conversion due to type coercion issues
    promptUsage = Number(promptUsage);
    dependencyUsage = Number(dependencyUsage);

    const classes = useClasses();
    const MAX_WIDTH = 500;
    const totalUsage = promptUsage + dependencyUsage;
    const promptPercentage = promptUsage / totalUsage;
    const dependencyPercentage = dependencyUsage / totalUsage;

    const promptWidth = promptPercentage * MAX_WIDTH;
    const dependencyWidth = dependencyPercentage * MAX_WIDTH;

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
                            <Body1Strong>Prompt token usage</Body1Strong> is the number of tokens used in the bot
                            generation prompt. <Body1Strong>Dependency token usage</Body1Strong> is the number of tokens
                            used in dependency prompts called to construct the bot generation prompt.
                        </Body1>
                    </PopoverSurface>
                </Popover>
            </h3>
            <div className={classes.content}>
                {hasTokenCount ? (
                    <>
                        {totalUsage > 0 ? (
                            <>
                                {!promptView && <Text>Total token usage for current session</Text>}
                                <div className={classes.horizontal} style={{ gap: tokens.spacingHorizontalXXS }}>
                                    <div
                                        style={{
                                            backgroundColor: tokens.colorNeutralForeground2BrandHover,
                                            height: '10px',
                                            width: `${promptWidth}px`,
                                        }}
                                    />
                                    <div
                                        style={{
                                            backgroundColor: tokens.colorPaletteRedBackground2,
                                            height: '10px',
                                            width: `${dependencyWidth}px`,
                                        }}
                                    />
                                </div>
                            </>
                        ) : promptView ? (
                            <Text>No tokens were used. This is a hardcoded response.</Text>
                        ) : (
                            <Text>No tokens have been used in this session yet.</Text>
                        )}
                        <div className={classes.horizontal}>
                            <div
                                style={{
                                    backgroundColor: tokens.colorNeutralForeground2BrandHover,
                                    height: '10px',
                                    width: `10px`,
                                }}
                            />
                            <Text>
                                Prompt {'('}
                                {promptUsage}
                                {')'}
                            </Text>
                            <div
                                style={{
                                    backgroundColor: tokens.colorPaletteRedBackground2,
                                    height: '10px',
                                    width: `10px`,
                                }}
                            />
                            <Text>
                                Dependency {'('}
                                {dependencyUsage}
                                {')'}
                            </Text>
                        </div>
                    </>
                ) : loadingResponse ? (
                    <Body1>
                        Token usage will be calculated once response is generated.
                        <TypingIndicator />
                    </Body1>
                ) : (
                    <Body2>Unable to determine tokens used for this prompt.</Body2>
                )}
            </div>
            <Divider />
        </>
    );
};
