import { AccordionHeader, AccordionItem, AccordionPanel, Body1, Body2 } from '@fluentui/react-components';
import { StepwiseStep } from '../../../../libs/models/StepwiseStep';

interface IStepwiseStepViewProps {
    step: StepwiseStep;
}

export const StepwiseStepView: React.FC<IStepwiseStepViewProps> = ({ step }) => {
    let header = `[OBSERVATION] ${step.observation}`;
    let details: string | undefined;

    if (step.action) {
        header = `[ACTION] ${step.action}`;
        details = step.action_variables
            ? Object.entries(step.action_variables)
                  .map(([key, value]) => `${key}: ${value}`)
                  .join('\n')
            : '';

        const observation = step.observation?.replace(/[\\r]/g, '').replace(/\\{1,2}u0022/g, '"');
        details = details.concat(`\nObservation: ${observation}`);
    }

    if (step.thought) {
        const thoughtRegEx = /\[(THOUGHT|QUESTION|ACTION)\]\s*(.*)$/g;
        header = step.thought.match(thoughtRegEx)?.[0].replace(/\\n/g, ' ') ?? step.thought;
    }

    return (
        <AccordionItem value={header}>
            {step.action ? (
                <>
                    <AccordionHeader expandIconPosition="end">
                        <Body2>{header}</Body2>
                    </AccordionHeader>
                    <AccordionPanel>
                        <Body1>
                            {details
                                ?.split('\n')
                                .map((paragraph, idx) => <p key={`step-details-${idx}`}>{paragraph}</p>)}
                        </Body1>
                    </AccordionPanel>
                </>
            ) : (
                <Body2>{header}</Body2>
            )}
        </AccordionItem>
    );
};
