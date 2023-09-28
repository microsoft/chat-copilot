import { Text, makeStyles, shorthands, tokens } from '@fluentui/react-components';
import React from 'react';
import { Plan, PlanState } from '../../../libs/models/Plan';
import { PlanStepCard } from './PlanStepCard';

const useClasses = makeStyles({
    root: {
        ...shorthands.gap(tokens.spacingVerticalM),
        display: 'flex',
        flexDirection: 'column',
        width: '100%',
    },
});

interface IPlanBodyProps {
    plan: Plan;
    setPlan: React.Dispatch<any>;
    planState: PlanState;
    description: string;
}

export const PlanBody: React.FC<IPlanBodyProps> = ({ plan, setPlan, planState, description }) => {
    const classes = useClasses();

    const onDeleteStep = (index: number) => {
        setPlan({
            ...plan,
            steps: plan.steps.filter((_step: Plan, i: number) => i !== index),
        });
    };

    return (
        <div className={classes.root}>
            <Text weight="bold">{`Goal: ${description}`}</Text>
            {plan.steps.map((step: any, index: number) => {
                return (
                    <PlanStepCard
                        key={`Plan step: ${index}`}
                        step={{ ...step, index } as Plan}
                        enableEdits={planState === PlanState.PlanApprovalRequired || planState === PlanState.Derived}
                        enableStepDelete={plan.steps.length > 1}
                        onDeleteStep={onDeleteStep}
                    />
                );
            })}
        </div>
    );
};
