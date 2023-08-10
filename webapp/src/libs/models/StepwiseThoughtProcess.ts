import { PlanType } from './Plan';
import { StepwiseStep } from './StepwiseStep';

// Information about a pass through stepwise planner.
export interface StepwiseThoughtProcess {
    // Steps taken execution stat.
    stepsTaken: StepwiseStep[];

    // Time taken to fulfil the goal.
    timeTaken: string;

    // Skills used execution stat.
    skillsUsed: string;

    // Planner type.
    plannerType: PlanType;
}
