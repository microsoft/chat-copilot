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
        width: '100%',
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
});

interface IStepwiseStepViewProps {
    step: StepwiseStep;
    index: number;
}

export const StepwiseStepView: React.FC<IStepwiseStepViewProps> = ({ step, index }) => {
    const classes = useClasses();

    let header = `[OBSERVATION] ${step.observation}`;
    let details: string | undefined;

    if (step.action) {
        header = `[ACTION] ${step.action}`;
        details = step.action_variables
            ? 'Action variables: \n' +
              Object.entries(step.action_variables)
                  .map(([key, value]) => `${key}: ${value}`)
                  .join('\n')
            : '';
        const observation = step.observation?.replace(/[\\r]/g, '').replace(/\\{1,2}u0022/g, '"');
        details = details.concat(`\nObservation: \n${observation}`);
    }

    if (step.thought) {
        const thoughtRegEx = /\[(THOUGHT|QUESTION|ACTION)]\s*(.*)/g;
        let thought = step.thought.match(thoughtRegEx)?.[0].replace(/\\n/g, ' ') ?? `[THOUGHT] ${step.thought}`;
        const sentences = thought.split('. ');
        if (sentences.length > 1) {
            thought = sentences.length > 1 ? sentences[0] + '.' : thought;
            details = sentences.slice(1).join('. ');
        }
        header = thought;
    }

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
                            <Body1>
                                {details.split('\n').map((paragraph, idx) => (
                                    <p key={`step-details-${idx}`}>{paragraph}</p>
                                ))}
                            </Body1>
                        </AccordionPanel>
                    </>
                ) : (
                    <Body1>{header}</Body1>
                )}
            </AccordionItem>
        </div>
    );
};
