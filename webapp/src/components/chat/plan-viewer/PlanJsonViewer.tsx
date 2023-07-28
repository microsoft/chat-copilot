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
} from '@fluentui/react-components';
import React from 'react';

interface IPlanJsonViewerProps {
    goal: string;
    json: string;
}

export const PlanJsonViewer: React.FC<IPlanJsonViewerProps> = ({ goal, json }) => {
    return (
        <Dialog>
            <DialogTrigger disableButtonEnhancement>
                <Link>{goal}</Link>
            </DialogTrigger>
            <DialogSurface>
                <DialogBody>
                    <DialogTitle>Plan in JSON format</DialogTitle>
                    <DialogContent>
                        <pre>
                            <code>{JSON.stringify(JSON.parse(json), null, 2)}</code>
                        </pre>
                    </DialogContent>
                    <DialogActions>
                        <DialogTrigger disableButtonEnhancement>
                            <Button appearance="secondary">Close</Button>
                        </DialogTrigger>
                    </DialogActions>
                </DialogBody>
            </DialogSurface>
        </Dialog>
    );
};
