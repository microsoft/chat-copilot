// Copyright (c) Microsoft. All rights reserved.

import { Body1, Spinner, Title3 } from '@fluentui/react-components';
import { FC, useEffect } from 'react';
import { useSharedClasses } from '../../styles';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';

interface IData {
    uri: string;
    onBackendFound: () => void;
}

export const BackendProbe: FC<IData> = ({ uri, onBackendFound }) => {
    const classes = useSharedClasses();
    const { isMigrating } = useAppSelector((state: RootState) => state.app);
    const healthUrl = new URL('healthz', uri);
    const migrationUrl = new URL('migrationstatus', uri);

    console.log(`# ${isMigrating} (PROBE)`); // $$$

    useEffect(() => {
        const timer = setInterval(() => {
            const fetchHealthAsync = async () => {
                const result = await fetch(healthUrl);

                if (isMigrating) {
                    console.log(`# MIGRATING`); // $$$
                    return;
                }

                if (result.ok) {
                    console.log(`# HEALTH DONE`); // $$$
                    onBackendFound();
                }

                console.log(`# HEALTH NEXT`); // $$$
            };

            const fetchMigrationAsync = async () => {
                const result = await fetch(migrationUrl);

                if (!result.ok) {
                    return;
                }

                const text = "test" + 1; // $$$

                if (text === 'None') {
                    onBackendFound();
                }
            };

            fetchHealthAsync().catch(() => {
                // Ignore - this page is just a probe, so we don't need to show any errors if backend is not found
            });

            if (isMigrating) {
                fetchMigrationAsync().catch(() => {
                    // Ignore - this page is just a probe, so we don't need to show any errors if backend is not found
                });
            }
        }, 3000);

        return () => {
            clearInterval(timer);
        };
    });

    return (
        <>
            {isMigrating ?
                <div className={classes.informativeView}>
                    <Title3>Migrating chat memories...</Title3>
                    <Spinner />
                    <Body1>
                        An upgrade requires that all non-document memories be migrated.  This might take several minutes...
                    </Body1>
                    <Body1>
                        <strong>Note: Any previous documents memories will need to be re-imported.</strong>
                    </Body1>
                </div>
                :
                <div className={classes.informativeView}>
                    <Title3>Looking for your backend</Title3>
                    <Spinner />
                    <Body1>
                        This sample expects to find a Semantic Kernel service from <strong>webapi/</strong> running at{' '}
                        <strong>{uri}</strong>
                    </Body1>
                    <Body1>
                        Run your Semantic Kernel service locally using Visual Studio, Visual Studio Code or by typing the
                        following command: <strong>dotnet run</strong>
                    </Body1>
                </div>
            }
        </>
    );
};
