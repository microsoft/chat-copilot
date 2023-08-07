import { Accordion, Body1, makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { StepwiseStep } from '../../../../libs/models/StepwiseStep';
import { StepwiseStepView } from './StepwiseStepView';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingHorizontalSNudge),
    },
    header: {
        paddingTop: tokens.spacingVerticalS,
    },
});

interface IStepwiseThoughtProcessProps {
    stepwiseResult: string;
}

export const StepwiseThoughtProcess: React.FC<IStepwiseThoughtProcessProps> = ({ stepwiseResult }) => {
    const classes = useClasses();

    const thoughtProcessRegEx = /Result not found, review _stepsTaken to see what happened\.\s+(\[{.*}])/g;
    const matches = stepwiseResult.matchAll(thoughtProcessRegEx);
    const match = Array.from(matches);
    if (match.length > 0) {
        const steps = JSON.parse(match[0][1]) as StepwiseStep[];
        return (
            <div className={mergeClasses(classes.root, classes.header)}>
                <Body1>[THOUGHT PROCESS]</Body1>
                <Accordion collapsible multiple className={classes.root}>
                    {steps.map((step, index) => {
                        return <StepwiseStepView step={step} key={`stepwise-thought-${index}`} index={index} />;
                    })}
                </Accordion>
            </div>
        );
    }
    return;
};
