import { Button, Text, makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { CheckmarkCircle24Regular, DismissCircle24Regular, Info24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { GetResponseOptions } from '../../../libs/hooks/useChat';
import { ChatMessageType, IChatMessage } from '../../../libs/models/ChatMessage';
import { PlanState, ProposedPlan } from '../../../libs/models/Plan';
import { ContextVariable } from '../../../libs/semantic-kernel/model/AskResult';
import { getPlanGoal } from '../../../libs/utils/PlanUtils';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { updateMessageProperty } from '../../../redux/features/conversations/conversationsSlice';
import { PlanBody } from './PlanBody';

const useClasses = makeStyles({
    container: {
        ...shorthands.gap(tokens.spacingVerticalM),
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'baseline',
    },
    buttons: {
        display: 'flex',
        flexDirection: 'row',
        marginTop: tokens.spacingVerticalM,
        marginBottom: tokens.spacingVerticalM,
        ...shorthands.gap(tokens.spacingHorizontalL),
    },
    status: {
        ...shorthands.gap(tokens.spacingHorizontalMNudge),
    },
    text: {
        alignSelf: 'center',
    },
});

interface PlanViewerProps {
    message: IChatMessage;
    messageIndex: number;
    getResponse: (options: GetResponseOptions) => Promise<void>;
}

/* eslint-disable 
    @typescript-eslint/no-unsafe-assignment,
    @typescript-eslint/no-unsafe-member-access,
    @typescript-eslint/no-unsafe-call,
*/
export const PlanViewer: React.FC<PlanViewerProps> = ({ message, messageIndex, getResponse }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { selectedId } = useAppSelector((state: RootState) => state.conversations);

    // Track original plan from user message
    const parsedContent: ProposedPlan = JSON.parse(message.content);
    const originalPlan = parsedContent.proposedPlan;
    const description = getPlanGoal(originalPlan.description);
    const planState = message.planState ?? parsedContent.state;
    const [plan, setPlan] = useState(originalPlan);

    const onPlanAction = async (planState: PlanState.PlanApproved | PlanState.PlanRejected) => {
        const updatedPlan = JSON.stringify({
            proposedPlan: plan,
            type: parsedContent.type,
            state: planState,
        });

        dispatch(
            updateMessageProperty({
                messageIdOrIndex: messageIndex,
                chatId: selectedId,
                property: 'planState',
                value: planState,
                updatedContent: updatedPlan,
                frontLoad: true,
            }),
        );

        const contextVariables: ContextVariable[] = [
            {
                key: 'responseMessageId',
                value: message.id ?? '',
            },
            {
                key: 'proposedPlan',
                value: updatedPlan,
            },
        ];

        contextVariables.push(
            planState === PlanState.PlanApproved
                ? {
                      key: 'planUserIntent',
                      value: description,
                  }
                : {
                      key: 'userCancelledPlan',
                      value: 'true',
                  },
        );

        // Invoke plan
        await getResponse({
            value: planState === PlanState.PlanApproved ? 'Yes, proceed' : 'No, cancel',
            contextVariables,
            messageType: ChatMessageType.Message,
            chatId: selectedId,
        });
    };

    return (
        <div className={classes.container}>
            <Text>Based on the request, Copilot Chat will run the following steps:</Text>
            <PlanBody plan={plan} setPlan={setPlan} planState={planState} />
            {planState === PlanState.PlanApprovalRequired && (
                <>
                    Would you like to proceed with the plan?
                    <div className={classes.buttons}>
                        <Button
                            data-testid="cancelPlanButton"
                            appearance="secondary"
                            onClick={() => {
                                void onPlanAction(PlanState.PlanRejected);
                            }}
                        >
                            No, cancel plan
                        </Button>
                        <Button
                            data-testid="proceedWithPlanButton"
                            type="submit"
                            appearance="primary"
                            onClick={() => {
                                void onPlanAction(PlanState.PlanApproved);
                            }}
                        >
                            Yes, proceed
                        </Button>
                    </div>
                </>
            )}
            {planState === PlanState.PlanApproved && (
                <div className={mergeClasses(classes.buttons, classes.status)}>
                    <CheckmarkCircle24Regular />
                    <Text className={classes.text}> Plan Executed</Text>
                </div>
            )}
            {planState === PlanState.PlanRejected && (
                <div className={mergeClasses(classes.buttons, classes.status)}>
                    <DismissCircle24Regular />
                    <Text className={classes.text}> Plan Cancelled</Text>
                </div>
            )}
            {(planState === PlanState.NoOp || planState === PlanState.Disabled) && (
                <div className={mergeClasses(classes.buttons, classes.status)}>
                    <Info24Regular />
                    <Text className={classes.text}>
                        {planState === PlanState.NoOp
                            ? 'Your app state has changed since this plan was generated, making it unreliable for the planner. Please request a fresh plan to avoid potential conflicts.'
                            : 'Only the person who prompted this plan can take action on it.'}
                    </Text>
                </div>
            )}
        </div>
    );
};
