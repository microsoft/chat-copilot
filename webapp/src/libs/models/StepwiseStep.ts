export interface StepwiseStep {
    // The step number
    thought: string;

    // The action of the step
    action?: string;

    // The variables for the action
    action_variables?: Record<string, string>;

    // The output of the action
    observation?: string;

    // The output of the system
    final_answer?: string;

    // The raw response from the action
    original_response: string;
}
