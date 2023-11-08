import { PlanType } from './Plan';

// Metadata about plan execution.
export interface PlanExecutionMetadata {
    // Steps taken execution stat.
    stepsTaken: string;

    // Time taken to fulfil the goal.
    timeTaken: string;

    // Functions used execution stat.
    functionsUsed: string;

    // Planner type.
    plannerType: PlanType;
}
