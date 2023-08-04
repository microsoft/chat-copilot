import { Accordion, Body2 } from '@fluentui/react-components';
import { StepwiseStep } from '../../../../libs/models/StepwiseStep';
import { StepwiseStepView } from './StepwiseStepView';

interface IStepwiseThoughtProcessProps {
    stepwiseResult: string;
}

export const StepwiseThoughtProcess: React.FC<IStepwiseThoughtProcessProps> = ({ stepwiseResult }) => {
    const thoughtProcessRegEx = /Result not found, review _stepsTaken to see what happened\.\s+(\[{.*}])/g;
    const matches = stepwiseResult.matchAll(thoughtProcessRegEx);
    const match = Array.from(matches);
    if (match.length > 0) {
        const steps = JSON.parse(match[0][1]) as StepwiseStep[];
        return (
            <div>
                <Body2>[THOUGHT PROCESS]</Body2>
                <Accordion collapsible multiple>
                    {steps.map((step, index) => {
                        return <StepwiseStepView step={step} key={`stepwise-thought-${index}`} />;
                    })}
                </Accordion>
            </div>
        );
    }
    return;
};
