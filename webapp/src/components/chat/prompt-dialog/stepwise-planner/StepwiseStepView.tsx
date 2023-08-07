import {
    AccordionHeader,
    AccordionItem,
    AccordionPanel,
    Body1,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { StepwiseStep } from '../../../../libs/models/StepwiseStep';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingHorizontalM),
    },
    accordionItem: {
        width: '99%',
    },
    header: {
        width: '100%',
        /* Styles for the button within the header */
        '& button': {
            alignItems: 'flex-start',
            minHeight: '-webkit-fill-available',
            paddingLeft: tokens.spacingHorizontalNone,
        },
    },
    tab: {
        display: 'flex',
        marginLeft: tokens.spacingHorizontalL,
    },
});

interface IStepwiseStepViewProps {
    step: StepwiseStep;
    index: number;
}

export const StepwiseStepView: React.FC<IStepwiseStepViewProps> = ({ step, index }) => {
    const classes = useClasses();

    let header = `[OBSERVATION] ${step.observation}`;
    let details: string | undefined;

    if (step.thought) {
        const thoughtRegEx = /\[(THOUGHT|QUESTION|ACTION)](\s*(.*))*/g;
        const match = step.thought.match(thoughtRegEx);
        let thought = match?.[0].replace(/\\n/g, ' ') ?? `[THOUGHT] ${step.thought}`;
        const sentences = thought.split('. ');
        if (sentences.length > 1) {
            thought = sentences.length > 1 ? sentences[0] + '.' : thought;
            details = sentences.slice(1).join('. ');
        }
        header = thought;
    }

    if (step.action) {
        header = `[ACTION] ${step.action}`;
        const variables = step.action_variables
            ? 'Action variables: \n' +
              Object.entries(step.action_variables)
                  .map(([key, value]) => `\r${key}: ${value}`)
                  .join('\n')
            : '';
        details = step.thought.replace('[ACTION]', '').replaceAll('```', '') + '\n';
        const observation = step.observation?.replaceAll(/\\{0,2}u0022/g, '"');
        details = details.concat(variables, `\nObservation: \n\r${observation}`);
    }

    details = details?.replaceAll('\r\n', '\n\r');

    return (
        <div className={classes.root}>
            <Body1>{index + 1}.</Body1>
            <AccordionItem value={index} className={classes.accordionItem}>
                {details ? (
                    <>
                        <AccordionHeader expandIconPosition="end" className={classes.header}>
                            <Body1>{header}</Body1>
                        </AccordionHeader>
                        <AccordionPanel>
                            {
                                <Body1>
                                    {details.split('\n').map((paragraph, idx) => (
                                        <p
                                            key={`step-details-${idx}`}
                                            className={paragraph.includes('\r') ? classes.tab : undefined}
                                        >
                                            {paragraph}
                                        </p>
                                    ))}
                                </Body1>
                            }
                        </AccordionPanel>
                    </>
                ) : (
                    <Body1>{header}</Body1>
                )}
            </AccordionItem>
        </div>
    );
};
