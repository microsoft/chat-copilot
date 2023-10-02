export interface IPlanInput {
    // These have to be capitalized to match the server response
    Key: string;
    Value: string;
}

export enum PlanState {
    NoOp,
    Approved,
    Rejected,
    Derived,
    PlanApprovalRequired,
    Disabled,
}

export enum PlanType {
    Action, // single-step
    Sequential, // multi-step
    Stepwise, // MRKL style planning
}

/*
 * See Semantic Kernel's `Plan` object below for full definition.
 * Not explicitly defining the type here to avoid additional overhead of property maintenance.
 * https://github.com/microsoft/semantic-kernel/blob/df07fc6f28853a481dd6f47e60d39a52fc6c9967/dotnet/src/SemanticKernel/Planning/Plan.cs#
 */
export interface Plan {
    // State of the plan
    state: IPlanInput[];

    // Steps of the plan
    steps: Plan[];

    // Parameters for the plan, used to pass information to the next step
    parameters: IPlanInput[];

    // Outputs for the plan, used to pass information to the caller
    outputs: string[];

    // Gets whether the plan has a next step
    hasNextStep: boolean;

    // Gets the next step index
    nextStepIndex: number;

    // Name of the plan
    name: string;

    // Name of the skill
    skill_name: string;

    // Description of the plan
    description: string;

    // Whether the plan is semantic
    isSemantic: boolean;

    // Whether the plan is sensitive
    isSensitive: boolean;

    // The trust service instance for the plan
    trustServiceInstance: any;

    // The request settings for the plan
    requestSettings: any;

    // Step
    index: number;
}

// Information about a single proposed plan from Chat Copilot
export interface ProposedPlan {
    // Plan object to be approved or invoked
    proposedPlan: Plan;

    // Indicates whether plan is Action (single-step) or Sequential (multi-step)
    type: PlanType;

    // State of plan
    state: PlanState;

    // User input that prompted the plan
    originalUserInput: string;

    // User intent to serves as goal of plan.
    userIntent?: string;

    // Id tracking bot message of generated plan in chat history
    generatedPlanMessageId?: string;
}
