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
import { formatParagraphTextContent } from '../../../utils/TextUtils';

export const useStepClasses = makeStyles({
    root: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingHorizontalM),
    },
    accordionItem: {
        width: '95%',
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
    const classes = useStepClasses();

    let header = `[OBSERVATION] ${step.observation}`;
    let details: string | undefined;

    if (step.thought || step.final_answer) {
        const thoughtRegEx = /\[(THOUGHT|QUESTION|ACTION)](\s*(.*))*/g;
        let thought = step.final_answer
            ? `[FINAL ANSWER] ${step.final_answer}`
            : step.thought.match(thoughtRegEx)?.[0] ?? `[THOUGHT] ${step.thought}`;

        // Only show the first sentence of the thought in the header. Show the rest as details.
        // Match the first period or colon followed by a non-digit or non-letter
        const firstSentenceIndex = thought.search(/(\.|:)([^a-z\d]|$)/);
        if (firstSentenceIndex > 0) {
            details = thought.substring(firstSentenceIndex + 2);
            thought = thought.substring(0, firstSentenceIndex + 1);
        }

        header = thought;
    }

    if (step.action) {
        header = `[ACTION] ${step.action}`;

        // Format the action variables and observation.
        const variables = step.action_variables
            ? 'Action variables: \n' +
              Object.entries(step.action_variables)
                  .map(([key, value]) => `\r${key}: ${value}`)
                  .join('\n')
            : '';

        // Remove the [ACTION] tag from the thought and remove any code block formatting.
        details = step.thought.replace('[ACTION]', '').replaceAll('```', '') + '\n';

        // Parse any unicode quotation characters in the observation.
        const observation = step.observation?.replaceAll(/\\{0,2}u0022/g, '"');
        details = details.concat(variables, `\nObservation: \n\r${observation}`);
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
                        <AccordionPanel>{formatParagraphTextContent(details)}</AccordionPanel>
                    </>
                ) : (
                    <Body1>{header}</Body1>
                )}
            </AccordionItem>
        </div>
    );
};
