// Copyright (c) Microsoft. All rights reserved.

import { Body1, Spinner, Title3 } from '@fluentui/react-components';
import { FC, useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { setMaintenance } from '../../redux/features/app/appSlice';
import { useSharedClasses } from '../../styles';

interface IData {
    uri: string;
    onBackendFound: () => void;
}

interface IMaintenance {
    title: string | null;
    message: string | null;
    note: string | null | undefined;
}

export const BackendProbe: FC<IData> = ({ uri, onBackendFound }) => {
    const classes = useSharedClasses();
    const dispatch = useAppDispatch();
    const { isMaintenance } = useAppSelector((state: RootState) => state.app);
    const healthUrl = new URL('healthz', uri);
    const migrationUrl = new URL('maintenanceStatus', uri);

    const [model, setModel] = useState<IMaintenance | null>(null);

    useEffect(() => {
        const timer = setInterval(() => {
            const fetchHealthAsync = async () => {
                const result = await fetch(healthUrl);

                if (result.ok) {
                    onBackendFound();
                }
            };

            const fetchMaintenanceAsync = async () => {
                const result = await fetch(migrationUrl);

                if (!result.ok) {
                    return;
                }

                // Parse json from body
                result
                    .json()
                    .then((data) => {
                        // Body has payload.  This means the app is in maintenance
                        setModel(data as IMaintenance);
                    })
                    .catch(() => {
                        // JSON Exception since response has no body.  This means app is not in maintenance.
                        dispatch(setMaintenance(false));
                        onBackendFound();
                    });
            };

            if (!isMaintenance) {
                fetchHealthAsync().catch(() => {
                    // Ignore - this page is just a probe, so we don't need to show any errors if backend is not found
                });
            }

            fetchMaintenanceAsync().catch(() => {
                // Ignore - this page is just a probe, so we don't need to show any errors if backend is not found
            });
        }, 3000);

        return () => {
            clearInterval(timer);
        };
    });

    return (
        <>
            {isMaintenance ? (
                <div className={classes.informativeView}>
                    <Title3>{model?.title ?? 'Site undergoing maintenance...'}</Title3>
                    <Spinner />
                    <Body1>
                        {model?.message ?? 'Planned site maintenance is underway.  We apologize for the disruption.'}
                    </Body1>
                    <Body1>
                        <strong>
                            {model?.note ??
                                "Note: If this message doesn't resolve after a significant duration, refresh the browser."}
                        </strong>
                    </Body1>
                </div>
            ) : (
                <div className={classes.informativeView}>
                    <Title3>Looking for your backend</Title3>
                    <Spinner />
                    <Body1>
                        This sample expects to find a Semantic Kernel service from <strong>webapi/</strong> running at{' '}
                        <strong>{uri}</strong>
                    </Body1>
                    <Body1>
                        Run your Semantic Kernel service locally using Visual Studio, Visual Studio Code or by typing
                        the following command: <strong>dotnet run</strong>
                    </Body1>
                </div>
            )}
        </>
    );
};
