// Copyright (c) Microsoft. All rights reserved.

import { Body1, Spinner, Title3 } from '@fluentui/react-components';
import { FC, useEffect, useRef } from 'react';
import { useSharedClasses } from '../../styles';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { setMaintenance } from '../../redux/features/app/appSlice';

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
    const migrationUrl = new URL('maintenancestatus', uri);

    const model = useRef<IMaintenance | null>(null);

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

                const json: unknown = await result.json();

                if (json === null) {
                    dispatch(setMaintenance(false));
                    onBackendFound();
                }

                model.current = json as IMaintenance | null;
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
                    <Title3>{model.current?.title ?? 'Site undergoing maintenance...'}</Title3>
                    <Spinner />
                    <Body1>
                        {model.current?.message ??
                            'Planned site maintenance is underway.  We apologize for the disruption.'}
                    </Body1>
                    <Body1>
                        <strong>
                            {model.current?.note ??
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
