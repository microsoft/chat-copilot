// Copyright (c) Microsoft. All rights reserved.

import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { Alert } from '@fluentui/react-components/unstable';
import React from 'react';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { removeAlert } from '../../redux/features/app/appSlice';
import { Dismiss16 } from './BundledIcons';

const useClasses = makeStyles({
    alert: {
        fontWeight: tokens.fontWeightRegular,
        color: tokens.colorNeutralForeground1,
        backgroundColor: tokens.colorNeutralBackground6,
        fontSize: tokens.fontSizeBase200,
        lineHeight: tokens.lineHeightBase200,
    },
    actionItems: {
        display: 'flex',
        flexDirection: 'row',
        ...shorthands.gap(tokens.spacingHorizontalMNudge),
    },
    button: {
        alignSelf: 'center',
    },
});

export const Alerts: React.FC = () => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { alerts } = useAppSelector((state: RootState) => state.app);

    return (
        <div>
            {alerts.map(({ type, message, onRetry }, index) => {
                return (
                    <Alert
                        intent={type}
                        action={{
                            children: (
                                <div className={classes.actionItems}>
                                    {onRetry && <div onClick={onRetry}>Retry</div>}
                                    <Dismiss16
                                        aria-label="dismiss message"
                                        onClick={() => {
                                            dispatch(removeAlert(index));
                                        }}
                                        color="black"
                                        className={classes.button}
                                    />
                                </div>
                            ),
                        }}
                        key={`${index}-${type}`}
                        className={classes.alert}
                    >
                        {message.slice(0, 1000) + (message.length > 1000 ? '...' : '')}
                    </Alert>
                );
            })}
        </div>
    );
};
