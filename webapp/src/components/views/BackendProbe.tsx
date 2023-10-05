// Copyright (c) Microsoft. All rights reserved.

import { Body1, Spinner, Title3 } from '@fluentui/react-components';
import { FC, useEffect, useMemo, useState } from 'react';
import { renderApp } from '../../index';
import { AuthHelper } from '../../libs/auth/AuthHelper';
import { BackendServiceUrl } from '../../libs/services/BaseService';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { setMaintenance } from '../../redux/features/app/appSlice';
import { useSharedClasses } from '../../styles';

interface IData {
    onBackendFound: () => void;
}

interface IMaintenance {
    title: string | null;
    message: string | null;
    note: string | null | undefined;
}

export const BackendProbe: FC<IData> = ({ onBackendFound }) => {
    const classes = useSharedClasses();
    const dispatch = useAppDispatch();
    const { isMaintenance } = useAppSelector((state: RootState) => state.app);
    const healthUrl = useMemo(() => new URL('healthz', BackendServiceUrl), []);
    const migrationUrl = useMemo(() => new URL('maintenanceStatus', BackendServiceUrl), []);

    const [model, setModel] = useState<IMaintenance | null>(null);

    useEffect(() => {
        const timer = setInterval(() => {
            const onBackendFoundWithAuthCheck = () => {
                if (!AuthHelper.getAuthConfig()) {
                    // if we don't have the auth config, re-render the app:
                    renderApp();
                } else {
                    // otherwise, we can load as normal
                    onBackendFound();
                }
            };

            const fetchHealthAsync = async () => {
                const result = await fetch(healthUrl);

                if (result.ok) {
                    onBackendFoundWithAuthCheck();
                }
            };

            const fetchMaintenanceAsync = () =>
                fetch(migrationUrl)
                    .then((response) => response.json())
                    .then((data) => {
                        // Body has payload. This means the app is in maintenance
                        setModel(data as IMaintenance);
                        clearInterval(timer);
                        return false;
                    })
                    .catch((e: any) => {
                        if (e instanceof TypeError) {
                            // fetch() will reject with a TypeError when a network error is encountered
                            // this means the backend is not found and we need to probe.
                            return true;
                        }

                        // JSON Exception since response has no body. This means app is not in maintenance.
                        dispatch(setMaintenance(false));
                        onBackendFoundWithAuthCheck();
                        return false;
                    });

            fetchMaintenanceAsync()
                .then((shouldProbe) => (shouldProbe ? fetchHealthAsync() : Promise.resolve()))
                .catch(() => {
                    // Ignore - this page is just a probe, so we don't need to show any errors if backend is not found
                });
        }, 3000);

        return () => {
            clearInterval(timer);
        };
    }, [dispatch, healthUrl, migrationUrl, onBackendFound]);

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
                        <strong>{BackendServiceUrl}</strong>
                    </Body1>
                    <Body1>
                        Run your Semantic Kernel service locally using Visual Studio, Visual Studio Code or by typing
                        the following command:{' '}
                        <code>
                            <strong>dotnet run</strong>
                        </code>
                    </Body1>
                    <Body1>
                        If running locally, ensure that you have the{' '}
                        <code>
                            <b>REACT_APP_BACKEND_URI</b>
                        </code>{' '}
                        variable set in your <b>webapp/.env</b> file
                    </Body1>
                </div>
            )}
        </>
    );
};
