import { Accordion, Body1, makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { Constants } from '../../../../Constants';
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

    const matches = stepwiseResult.matchAll(Constants.STEPWISE_RESULT_NOT_FOUND_REGEX);
    const matchGroups = Array.from(matches);
    if (matchGroups.length > 0) {
        const steps = JSON.parse(matchGroups[0][2]) as StepwiseStep[];
        return (
            <div className={mergeClasses(classes.root, classes.header)}>
                <Body1>{matchGroups[0][1]}</Body1>
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
