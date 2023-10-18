import {
    Accordion,
    AccordionHeader,
    Body1,
    Body1Strong,
    makeStyles,
    mergeClasses,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { useState } from 'react';
import { COPY } from '../../../../assets/strings';
import { DependencyDetails } from '../../../../libs/models/BotResponsePrompt';
import { PlanExecutionMetadata } from '../../../../libs/models/PlanExecutionMetadata';
import { StepwiseStep } from '../../../../libs/models/StepwiseStep';
import { formatParagraphTextContent } from '../../../utils/TextUtils';
import { StepwiseStepView, useStepClasses } from './StepwiseStepView';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingHorizontalSNudge),
        paddingTop: tokens.spacingVerticalS,
        paddingBottom: tokens.spacingVerticalS,
    },
});

interface IStepwiseThoughtProcessViewProps {
    thoughtProcess: DependencyDetails;
}

export const StepwiseThoughtProcessView: React.FC<IStepwiseThoughtProcessViewProps> = ({ thoughtProcess }) => {
    const classes = useClasses();
    const stepClasses = useStepClasses();
    const stepwiseDetails = thoughtProcess.context as PlanExecutionMetadata;
    const steps = JSON.parse(stepwiseDetails.stepsTaken) as StepwiseStep[];

    const testResultNotFound = thoughtProcess.result.matchAll(COPY.STEPWISE_RESULT_NOT_FOUND_REGEX);
    const matchGroups = Array.from(testResultNotFound);
    const resultNotFound = matchGroups.length > 0;
    if (resultNotFound) {
        // Extract result not found message. The rest is the same as stepsTaken.
        thoughtProcess.result = matchGroups[0][1];
    }

    const [showthoughtProcess, setShowThoughtProcess] = useState(resultNotFound);

    return (
        <div className={mergeClasses(classes.root)}>
            <Body1>{formatParagraphTextContent(thoughtProcess.result)}</Body1>
            {!resultNotFound && (
                <AccordionHeader
                    onClick={() => {
                        setShowThoughtProcess(!showthoughtProcess);
                    }}
                    expandIconPosition="end"
                    className={stepClasses.header}
                >
                    Explore how the stepwise planner reached this result! Click here to show the steps and logic.
                </AccordionHeader>
            )}
            {showthoughtProcess && (
                <>
                    <Body1Strong>Time Taken:</Body1Strong>
                    <Body1>{stepwiseDetails.timeTaken}</Body1>
                    <Body1Strong>Plugins Used:</Body1Strong>
                    <Body1>{!stepwiseDetails.skillsUsed.startsWith('0') ? stepwiseDetails.skillsUsed : 'None'}</Body1>
                </>
            )}
            {(resultNotFound || showthoughtProcess) && (
                <>
                    {<Body1Strong>Steps taken:</Body1Strong>}
                    <Body1>[THOUGHT PROCESS]</Body1>
                    <Accordion collapsible multiple className={classes.root}>
                        {steps.map((step: StepwiseStep, index: number) => {
                            return <StepwiseStepView step={step} key={`stepwise-thought-${index}`} index={index} />;
                        })}
                    </Accordion>
                </>
            )}
        </div>
    );
};
