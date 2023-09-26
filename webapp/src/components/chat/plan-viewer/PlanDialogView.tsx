// Copyright (c) Microsoft. All rights reserved.

import {
    Button,
    Dialog,
    DialogActions,
    DialogBody,
    DialogContent,
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
import { PlanState, ProposedPlan } from '../../../libs/models/Plan';
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
});

interface IPlanDialogViewProps {
    goal: string;
    plan: ProposedPlan;
}

export const PlanDialogView: React.FC<IPlanDialogViewProps> = ({ goal, plan }) => {
    const classes = useClasses();
    const dialogClasses = useDialogClasses();
    const [planView, setPlanView] = React.useState(plan.proposedPlan);

    const [selectedTab, setSelectedTab] = React.useState<TabValue>('formatted');
    const onTabSelect: SelectTabEventHandler = (_event, data) => {
        setSelectedTab(data.value);
    };

    return (
        <Dialog>
            <DialogTrigger disableButtonEnhancement>
                <Link>{goal}</Link>
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
                                        description={goal}
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
                        {selectedTab === 'formatted' && <Button appearance="primary">Run Plan</Button>}
                        <DialogTrigger disableButtonEnhancement>
                            <Button appearance="secondary">Close</Button>
                        </DialogTrigger>
                    </DialogActions>
                </DialogBody>
            </DialogSurface>
        </Dialog>
    );
};
