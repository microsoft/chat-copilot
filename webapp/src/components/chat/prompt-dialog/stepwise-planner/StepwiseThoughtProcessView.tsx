import { Accordion, Body1, makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { DependencyDetails } from '../../../../libs/models/BotResponsePrompt';
import { StepwiseStep } from '../../../../libs/models/StepwiseStep';
import { StepwiseThoughtProcess } from '../../../../libs/models/StepwiseThoughtProcess';
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

interface IStepwiseThoughtProcessViewProps {
    thoughtProcess: DependencyDetails;
}

export const StepwiseThoughtProcessView: React.FC<IStepwiseThoughtProcessViewProps> = ({ thoughtProcess }) => {
    const classes = useClasses();
    const steps = (thoughtProcess.context as StepwiseThoughtProcess).stepsTaken;
    return (
        <div className={mergeClasses(classes.root, classes.header)}>
            <Body1>[THOUGHT PROCESS]</Body1>
            <Accordion collapsible multiple className={classes.root}>
                {
                    // eslint-disable-next-line  @typescript-eslint/no-unsafe-call
                    steps.map((step: StepwiseStep, index: number) => {
                        return <StepwiseStepView step={step} key={`stepwise-thought-${index}`} index={index} />;
                    })
                }
            </Accordion>
        </div>
    );
};
