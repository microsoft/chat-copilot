// Copyright (c) Microsoft. All rights reserved.

import {
    Body1,
    Button,
    Dialog,
    DialogActions,
    DialogBody,
    DialogContent,
    DialogOpenChangeData,
    DialogSurface,
    DialogTitle,
    DialogTrigger,
    Link,
    SelectTabEventHandler,
    Tab,
    TabList,
    TabValue,
    makeStyles,
    tokens,
} from '@fluentui/react-components';
import React from 'react';
import { useChat } from '../../../libs/hooks/useChat';
import { PlanState, ProposedPlan } from '../../../libs/models/Plan';
import { getPlanGoal } from '../../../libs/utils/PlanUtils';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { useDialogClasses } from '../../../styles';
import { PlanBody } from './PlanBody';

const useClasses = makeStyles({
    body: {
        height: '700px',
    },
    planView: {
        paddingLeft: tokens.spacingHorizontalXXS,
        paddingTop: tokens.spacingVerticalMNudge,
    },
    footer: {
        paddingRight: tokens.spacingVerticalL,
    },
    confirmationDialog: {
        width: '525px',
        maxHeight: '700px',
    },
    confirmationDialogActions: {
        paddingTop: tokens.spacingVerticalL,
    },
});

interface IPlanDialogViewProps {
    goal: string;
    plan: ProposedPlan;
    setChatTab: () => void;
}

export const PlanDialogView: React.FC<IPlanDialogViewProps> = ({ goal, plan, setChatTab }) => {
    const classes = useClasses();
    const dialogClasses = useDialogClasses();

    const chat = useChat();
    const [planView, setPlanView] = React.useState(plan.proposedPlan);
    const { selectedId } = useAppSelector((state: RootState) => state.conversations);
    const description = getPlanGoal(goal);

    const [openConfirmationDialog, setOpenConfirmationDialog] = React.useState(false);

    const [selectedTab, setSelectedTab] = React.useState<TabValue>('formatted');
    const onTabSelect: SelectTabEventHandler = (_event, data) => {
        setSelectedTab(data.value);
    };

    const onPlanAction = async () => {
        setOpenConfirmationDialog(false);
        setChatTab();

        const updatedPlan = JSON.stringify({
            ...plan,
            proposedPlan: planView,
            state: PlanState.Derived,
            generatedPlanMessageId: null,
        });

        await chat.processPlan(selectedId, PlanState.Derived, updatedPlan, description);
    };

    return (
        <>
            <Dialog
                onOpenChange={(_ev, data: DialogOpenChangeData) => {
                    if (data.open) setPlanView(plan.proposedPlan);
                }}
            >
                <DialogTrigger disableButtonEnhancement>
                    <Link>{description}</Link>
                </DialogTrigger>
                <DialogSurface className={dialogClasses.surface}>
                    <DialogBody className={classes.body}>
                        <DialogTitle>Plan Details</DialogTitle>
                        <DialogContent className={dialogClasses.content}>
                            <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
                                <Tab data-testid="formatted" id="formatted" value="formatted">
                                    Formatted
                                </Tab>
                                <Tab data-testid="json" id="json" value="json">
                                    Original JSON
                                </Tab>
                            </TabList>
                            <div className={dialogClasses.innerContent}>
                                {selectedTab === 'formatted' && (
                                    <div className={classes.planView}>
                                        <PlanBody
                                            plan={planView}
                                            setPlan={setPlanView}
                                            planState={PlanState.Derived}
                                            description={description}
                                        />
                                    </div>
                                )}
                                {selectedTab === 'json' && (
                                    <pre className={dialogClasses.text}>
                                        <code>{JSON.stringify(plan.proposedPlan, null, 2)}</code>
                                    </pre>
                                )}
                            </div>
                        </DialogContent>
                        <DialogActions className={classes.footer}>
                            {selectedTab === 'formatted' && (
                                <DialogTrigger disableButtonEnhancement>
                                    <Button
                                        appearance="primary"
                                        onClick={() => {
                                            setOpenConfirmationDialog(true);
                                        }}
                                    >
                                        Run Plan
                                    </Button>
                                </DialogTrigger>
                            )}
                            <DialogTrigger disableButtonEnhancement>
                                <Button appearance="secondary">Close</Button>
                            </DialogTrigger>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>
            <Dialog open={openConfirmationDialog}>
                <DialogSurface className={classes.confirmationDialog}>
                    <DialogBody>
                        <DialogTitle>
                            Are you sure you want to run this plan?
                            <br />
                            <Body1>Please ensure all required plugins are enabled before proceeding.</Body1>
                        </DialogTitle>
                        <DialogContent className={dialogClasses.content}>
                            <div className={classes.planView}>
                                <PlanBody
                                    plan={planView}
                                    setPlan={setPlanView}
                                    planState={PlanState.Derived}
                                    description={description}
                                />
                            </div>
                        </DialogContent>
                        <DialogActions className={classes.confirmationDialogActions}>
                            <Button appearance="primary" onClick={() => void onPlanAction()}>
                                Yes, proceed
                            </Button>
                            <Button
                                appearance="secondary"
                                onClick={() => {
                                    setOpenConfirmationDialog(false);
                                }}
                            >
                                No, cancel
                            </Button>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>
        </>
    );
};
